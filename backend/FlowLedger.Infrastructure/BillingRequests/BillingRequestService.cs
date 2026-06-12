using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Domain.Rules;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.BillingRequests;

public sealed class BillingRequestService : IBillingRequestService
{
    private const int MaxPageSize = 100;

    private readonly FlowLedgerDbContext _dbContext;

    public BillingRequestService(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<BillingRequestListItemDto>> GetAsync(
        BillingRequestQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var requests = ApplyVisibility(_dbContext.BillingRequests.AsNoTracking(), currentUser)
            .Include(x => x.Customer)
            .AsQueryable();

        if (query.Status is not null)
        {
            requests = requests.Where(x => x.Status == query.Status);
        }

        if (query.CustomerId is not null)
        {
            requests = requests.Where(x => x.CustomerId == query.CustomerId);
        }

        if (query.AssignedToMe)
        {
            requests = requests.Where(x => x.AssignedToUserId == currentUser.Id);
        }

        if (query.CreatedByMe)
        {
            requests = requests.Where(x => x.CreatedByUserId == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            requests = requests.Where(x =>
                x.RequestNumber.Contains(search) ||
                x.Title.Contains(search) ||
                x.Customer.Name.Contains(search));
        }

        if (query.FromDate is not null)
        {
            requests = requests.Where(x => x.CreatedAtUtc >= query.FromDate);
        }

        if (query.UntilDate is not null)
        {
            requests = requests.Where(x => x.CreatedAtUtc <= query.UntilDate);
        }

        var totalCount = await requests.CountAsync(cancellationToken);
        var items = await requests
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BillingRequestListItemDto(
                x.Id,
                x.RequestNumber,
                x.Title,
                x.Customer.Name,
                x.Status,
                x.TotalAmount,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<BillingRequestListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<BillingRequestDetailDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var request = await LoadDetailQuery()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (request is null || !CanView(request, currentUser))
        {
            throw new KeyNotFoundException("Billing request was not found.");
        }

        return request.ToDetailDto(GetAvailableActions(request, currentUser));
    }

    public async Task<Guid> CreateAsync(CreateBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureSalesOrAdmin(currentUser);

        var customerExists = await _dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new InvalidOperationException("Customer was not found.");
        }

        var now = DateTime.UtcNow;
        var billingRequest = new BillingRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = await NextRequestNumberAsync(cancellationToken),
            CustomerId = request.CustomerId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = BillingRequestStatus.Draft,
            CreatedByUserId = currentUser.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        SetLineItemsAndAmounts(billingRequest, request.LineItems);
        AddAuditLog(billingRequest, currentUser.Id, AuditActionType.Created, "Billing request created.", now);

        _dbContext.BillingRequests.Add(billingRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return billingRequest.Id;
    }

    public async Task UpdateAsync(Guid id, UpdateBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureSalesOrAdmin(currentUser);

        var billingRequest = await _dbContext.BillingRequests
            .Include(x => x.LineItems)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (billingRequest is null || !CanEdit(billingRequest, currentUser))
        {
            throw new KeyNotFoundException("Billing request was not found.");
        }

        var customerExists = await _dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new InvalidOperationException("Customer was not found.");
        }

        var now = DateTime.UtcNow;
        billingRequest.CustomerId = request.CustomerId;
        billingRequest.Title = request.Title.Trim();
        billingRequest.Description = request.Description.Trim();
        billingRequest.UpdatedAtUtc = now;
        billingRequest.AssignedToUserId = null;

        _dbContext.BillingRequestLineItems.RemoveRange(billingRequest.LineItems);
        billingRequest.LineItems.Clear();
        SetLineItemsAndAmounts(billingRequest, request.LineItems);
        AddAuditLog(billingRequest, currentUser.Id, AuditActionType.Updated, "Billing request updated.", now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureSalesOrAdmin(currentUser);

        var billingRequest = await _dbContext.BillingRequests
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (billingRequest is null || !CanSubmit(billingRequest, currentUser))
        {
            throw new KeyNotFoundException("Billing request was not found.");
        }

        if (billingRequest.Status is not (BillingRequestStatus.Draft or BillingRequestStatus.Rejected))
        {
            throw new InvalidOperationException("Only draft or rejected requests can be submitted.");
        }

        var accountsUserId = await GetFirstActiveUserIdAsync(RoleName.Accounts, cancellationToken);
        var now = DateTime.UtcNow;
        billingRequest.Status = BillingRequestStatus.AccountsReview;
        billingRequest.AssignedToUserId = accountsUserId;
        billingRequest.SubmittedAtUtc = now;
        billingRequest.RejectedAtUtc = null;
        billingRequest.UpdatedAtUtc = now;
        AddAuditLog(billingRequest, currentUser.Id, AuditActionType.Submitted, "Billing request submitted to Accounts.", now);
        AddAuditLog(billingRequest, accountsUserId, AuditActionType.Assigned, "Billing request assigned to Accounts.", now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid id, ApproveBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var billingRequest = await _dbContext.BillingRequests
            .Include(x => x.Invoice)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (billingRequest is null || !CanView(billingRequest, currentUser))
        {
            throw new KeyNotFoundException("Billing request was not found.");
        }

        var now = DateTime.UtcNow;
        if (billingRequest.Status == BillingRequestStatus.AccountsReview)
        {
            EnsureAccountsOrAdmin(currentUser);
            if (billingRequest.TotalAmount <= ApprovalRules.ManagerApprovalThreshold)
            {
                billingRequest.Status = BillingRequestStatus.InvoiceGenerated;
                billingRequest.ApprovedAtUtc = now;
                billingRequest.UpdatedAtUtc = now;
                AddAuditLog(billingRequest, currentUser.Id, AuditActionType.Approved, "Accounts approved billing request.", now);
                AddOptionalComment(billingRequest.Id, currentUser.Id, request.Comment, now);
                await CreateInvoiceAsync(billingRequest, currentUser.Id, now, cancellationToken);
            }
            else
            {
                var managerUserId = await GetFirstActiveUserIdAsync(RoleName.Manager, cancellationToken);
                billingRequest.Status = BillingRequestStatus.ManagerApproval;
                billingRequest.AssignedToUserId = managerUserId;
                billingRequest.UpdatedAtUtc = now;
                AddAuditLog(billingRequest, currentUser.Id, AuditActionType.Approved, "Accounts approved billing request for manager review.", now);
                AddAuditLog(billingRequest, managerUserId, AuditActionType.Assigned, "Billing request assigned to Management.", now);
                AddOptionalComment(billingRequest.Id, currentUser.Id, request.Comment, now);
            }
        }
        else if (billingRequest.Status == BillingRequestStatus.ManagerApproval)
        {
            EnsureManagerOrAdmin(currentUser);
            billingRequest.Status = BillingRequestStatus.InvoiceGenerated;
            billingRequest.ApprovedAtUtc = now;
            billingRequest.UpdatedAtUtc = now;
            AddAuditLog(billingRequest, currentUser.Id, AuditActionType.Approved, "Management approved billing request.", now);
            AddOptionalComment(billingRequest.Id, currentUser.Id, request.Comment, now);
            await CreateInvoiceAsync(billingRequest, currentUser.Id, now, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Billing request is not waiting for approval.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid id, RejectBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var billingRequest = await _dbContext.BillingRequests
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (billingRequest is null || !CanView(billingRequest, currentUser))
        {
            throw new KeyNotFoundException("Billing request was not found.");
        }

        if (billingRequest.Status == BillingRequestStatus.AccountsReview)
        {
            EnsureAccountsOrAdmin(currentUser);
        }
        else if (billingRequest.Status == BillingRequestStatus.ManagerApproval)
        {
            EnsureManagerOrAdmin(currentUser);
        }
        else
        {
            throw new InvalidOperationException("Billing request is not waiting for rejection.");
        }

        var now = DateTime.UtcNow;
        billingRequest.Status = BillingRequestStatus.Rejected;
        billingRequest.AssignedToUserId = billingRequest.CreatedByUserId;
        billingRequest.RejectedAtUtc = now;
        billingRequest.UpdatedAtUtc = now;
        AddAuditLog(billingRequest, currentUser.Id, AuditActionType.Rejected, request.Reason.Trim(), now);
        AddOptionalComment(billingRequest.Id, currentUser.Id, request.Reason, now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCommentAsync(Guid id, AddCommentDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var billingRequest = await _dbContext.BillingRequests
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (billingRequest is null || !CanView(billingRequest, currentUser))
        {
            throw new KeyNotFoundException("Billing request was not found.");
        }

        var now = DateTime.UtcNow;
        AddOptionalComment(id, currentUser.Id, request.Body, now);
        AddAuditLog(billingRequest, currentUser.Id, AuditActionType.Commented, "Comment added.", now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<BillingRequest> LoadDetailQuery()
    {
        return _dbContext.BillingRequests.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.CreatedByUser)
            .Include(x => x.AssignedToUser)
            .Include(x => x.LineItems)
            .Include(x => x.Comments.OrderBy(c => c.CreatedAtUtc)).ThenInclude(x => x.AuthorUser)
            .Include(x => x.AuditLogs.OrderBy(a => a.CreatedAtUtc)).ThenInclude(x => x.ActorUser)
            .Include(x => x.Invoice);
    }

    private static IQueryable<BillingRequest> ApplyVisibility(IQueryable<BillingRequest> query, CurrentUser currentUser)
    {
        return currentUser.Role switch
        {
            RoleName.Admin => query,
            RoleName.Sales => query.Where(x => x.CreatedByUserId == currentUser.Id),
            RoleName.Accounts => query,
            RoleName.Manager => query,
            _ => query.Where(x => x.Id == Guid.Empty)
        };
    }

    private static bool CanView(BillingRequest request, CurrentUser currentUser)
    {
        return currentUser.Role switch
        {
            RoleName.Admin => true,
            RoleName.Sales => request.CreatedByUserId == currentUser.Id,
            RoleName.Accounts => true,
            RoleName.Manager => true,
            _ => false
        };
    }

    private static bool CanEdit(BillingRequest request, CurrentUser currentUser)
    {
        return request.Status is BillingRequestStatus.Draft or BillingRequestStatus.Rejected &&
            (currentUser.IsAdmin || request.CreatedByUserId == currentUser.Id);
    }

    private static bool CanSubmit(BillingRequest request, CurrentUser currentUser)
    {
        return currentUser.IsAdmin || request.CreatedByUserId == currentUser.Id;
    }

    private static IReadOnlyList<string> GetAvailableActions(BillingRequest request, CurrentUser currentUser)
    {
        var actions = new List<string>();

        if (CanEdit(request, currentUser))
        {
            actions.Add("Update");
        }

        if (request.Status is BillingRequestStatus.Draft or BillingRequestStatus.Rejected && CanSubmit(request, currentUser))
        {
            actions.Add("Submit");
        }

        if (request.Status == BillingRequestStatus.AccountsReview && currentUser.Role is RoleName.Accounts or RoleName.Admin)
        {
            actions.Add("Approve");
            actions.Add("Reject");
        }

        if (request.Status == BillingRequestStatus.ManagerApproval && currentUser.Role is RoleName.Manager or RoleName.Admin)
        {
            actions.Add("Approve");
            actions.Add("Reject");
        }

        actions.Add("Comment");
        return actions;
    }

    private void SetLineItemsAndAmounts(BillingRequest billingRequest, IReadOnlyList<CreateBillingRequestLineItemDto> lineItems)
    {
        var subtotal = 0m;
        foreach (var item in lineItems)
        {
            var lineTotal = Math.Round(item.Quantity * item.UnitPrice, 2);
            var lineItem = new BillingRequestLineItem
            {
                Id = Guid.NewGuid(),
                BillingRequestId = billingRequest.Id,
                Description = item.Description.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = lineTotal
            };

            subtotal += lineTotal;
            billingRequest.LineItems.Add(lineItem);
            _dbContext.BillingRequestLineItems.Add(lineItem);
        }

        billingRequest.SubtotalAmount = subtotal;
        billingRequest.VatAmount = Math.Round(billingRequest.SubtotalAmount * ApprovalRules.VatRate, 2);
        billingRequest.TotalAmount = billingRequest.SubtotalAmount + billingRequest.VatAmount;
    }

    private async Task CreateInvoiceAsync(
        BillingRequest billingRequest,
        Guid actorUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (billingRequest.Invoice is not null)
        {
            return;
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = await NextInvoiceNumberAsync(cancellationToken),
            BillingRequestId = billingRequest.Id,
            CustomerId = billingRequest.CustomerId,
            SubtotalAmount = billingRequest.SubtotalAmount,
            VatAmount = billingRequest.VatAmount,
            TotalAmount = billingRequest.TotalAmount,
            Status = InvoiceStatus.Issued,
            IssuedAtUtc = now,
            DueAtUtc = now.AddDays(30)
        };

        _dbContext.Invoices.Add(invoice);
        AddAuditLog(billingRequest, actorUserId, AuditActionType.InvoiceGenerated, "Invoice generated.", now);
    }

    private async Task<string> NextRequestNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"BR-{DateTime.UtcNow.Year}-";
        var lastNumber = await _dbContext.BillingRequests
            .Where(x => x.RequestNumber.StartsWith(prefix))
            .OrderByDescending(x => x.RequestNumber)
            .Select(x => x.RequestNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return $"{prefix}{NextSequence(lastNumber):0000}";
    }

    private async Task<string> NextInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"INV-{DateTime.UtcNow.Year}-";
        var lastNumber = await _dbContext.Invoices
            .Where(x => x.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(x => x.InvoiceNumber)
            .Select(x => x.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return $"{prefix}{NextSequence(lastNumber):0000}";
    }

    private static int NextSequence(string? lastNumber)
    {
        return lastNumber is not null && int.TryParse(lastNumber[^4..], out var parsed)
            ? parsed + 1
            : 1;
    }

    private async Task<Guid> GetFirstActiveUserIdAsync(RoleName role, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .Where(x => x.Role == role && x.IsActive)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
    }

    private void AddOptionalComment(Guid billingRequestId, Guid authorUserId, string? body, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        _dbContext.Comments.Add(new Comment
        {
            Id = Guid.NewGuid(),
            BillingRequestId = billingRequestId,
            AuthorUserId = authorUserId,
            Body = body.Trim(),
            CreatedAtUtc = now
        });
    }

    private void AddAuditLog(BillingRequest billingRequest, Guid actorUserId, AuditActionType actionType, string message, DateTime now)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            BillingRequestId = billingRequest.Id,
            ActorUserId = actorUserId,
            ActionType = actionType,
            Message = message,
            CreatedAtUtc = now
        });
    }

    private static void EnsureSalesOrAdmin(CurrentUser currentUser)
    {
        if (currentUser.Role is not (RoleName.Sales or RoleName.Admin))
        {
            throw new UnauthorizedAccessException("Only Sales or Admin users can perform this action.");
        }
    }

    private static void EnsureAccountsOrAdmin(CurrentUser currentUser)
    {
        if (currentUser.Role is not (RoleName.Accounts or RoleName.Admin))
        {
            throw new UnauthorizedAccessException("Only Accounts or Admin users can perform this action.");
        }
    }

    private static void EnsureManagerOrAdmin(CurrentUser currentUser)
    {
        if (currentUser.Role is not (RoleName.Manager or RoleName.Admin))
        {
            throw new UnauthorizedAccessException("Only Manager or Admin users can perform this action.");
        }
    }
}
