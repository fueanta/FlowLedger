using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Audit;
using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;
using FlowLedger.Application.Configuration;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Common;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.BillingRequests;

public sealed class BillingRequestService : IBillingRequestService
{
    private const int MaxExportRows = 5000;

    private readonly FlowLedgerDbContext _dbContext;
    private readonly ISystemSettingsService _settingsService;
    private readonly IWorkflowAuditWriter _auditWriter;
    private readonly ICsvExportService _csvExportService;

    public BillingRequestService(
        FlowLedgerDbContext dbContext,
        ISystemSettingsService settingsService,
        IWorkflowAuditWriter auditWriter,
        ICsvExportService csvExportService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _auditWriter = auditWriter;
        _csvExportService = csvExportService;
    }

    public async Task<PagedResult<BillingRequestListItemDto>> GetAsync(
        BillingRequestQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var page = PagingQueryGuard.Page(query.Page);
        var pageSize = PagingQueryGuard.PageSize(query.PageSize);
        var requests = BuildListQuery(query, currentUser);

        var totalCount = await requests.CountAsync(cancellationToken);
        var items = await ApplySort(requests, query.SortBy, query.SortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BillingRequestListItemDto(
                x.Id,
                x.RequestNumber,
                x.Title,
                x.Customer.Name,
                x.Status,
                x.AssignedQueue,
                x.AssignedAtUtc,
                x.LastWorkflowActionAtUtc,
                x.TotalAmount,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<BillingRequestListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<CsvResult> ExportCsvAsync(BillingRequestQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var rows = await ApplySort(BuildListQuery(query, currentUser), query.SortBy, query.SortDirection)
            .Take(MaxExportRows)
            .Select(x => new BillingRequestListItemDto(
                x.Id,
                x.RequestNumber,
                x.Title,
                x.Customer.Name,
                x.Status,
                x.AssignedQueue,
                x.AssignedAtUtc,
                x.LastWorkflowActionAtUtc,
                x.TotalAmount,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return _csvExportService.Export(
            $"billing-requests-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            rows,
            [
                new("Request Number", x => x.RequestNumber),
                new("Title", x => x.Title),
                new("Client", x => x.CustomerName),
                new("Status", x => x.Status),
                new("Queue", x => x.AssignedQueue),
                new("Amount", x => x.TotalAmount),
                new("Created At UTC", x => x.CreatedAtUtc),
                new("Updated At UTC", x => x.UpdatedAtUtc)
            ]);
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

        await EnsureActiveCustomerAsync(request.CustomerId, cancellationToken);

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
            AssignedToUserId = currentUser.Id,
            AssignedQueue = WorkflowQueue.Sales,
            AssignedAtUtc = now,
            LastWorkflowActionAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var settings = await _settingsService.GetAsync(cancellationToken);
        SetLineItemsAndAmounts(billingRequest, request.LineItems, settings.VatPercentage);
        AddAuditLog(billingRequest, currentUser, AuditActionType.Created, "Billing request created.", now);

        _dbContext.BillingRequests.Add(billingRequest);
        await SaveWorkflowAsync(cancellationToken);

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

        await EnsureActiveCustomerAsync(request.CustomerId, cancellationToken);

        var now = DateTime.UtcNow;
        billingRequest.CustomerId = request.CustomerId;
        billingRequest.Title = request.Title.Trim();
        billingRequest.Description = request.Description.Trim();
        billingRequest.UpdatedAtUtc = now;
        billingRequest.LastWorkflowActionAtUtc = now;
        AssignToSales(billingRequest, now);

        _dbContext.BillingRequestLineItems.RemoveRange(billingRequest.LineItems);
        billingRequest.LineItems.Clear();
        var settings = await _settingsService.GetAsync(cancellationToken);
        SetLineItemsAndAmounts(billingRequest, request.LineItems, settings.VatPercentage);
        AddAuditLog(billingRequest, currentUser, AuditActionType.Updated, "Billing request updated.", now);

        await SaveWorkflowAsync(cancellationToken);
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

        var beforeStatus = billingRequest.Status.ToString();
        var accountsUser = await GetFirstActiveUserAsync(RoleName.Accounts, cancellationToken);
        var now = DateTime.UtcNow;
        billingRequest.Status = BillingRequestStatus.AccountsReview;
        billingRequest.AssignedToUserId = accountsUser.Id;
        billingRequest.AssignedQueue = WorkflowQueue.Accounts;
        billingRequest.AssignedAtUtc = now;
        billingRequest.SubmittedByUserId = currentUser.Id;
        billingRequest.SubmittedAtUtc = now;
        billingRequest.RejectedAtUtc = null;
        billingRequest.UpdatedAtUtc = now;
        billingRequest.LastWorkflowActionAtUtc = now;
        AddAuditLog(billingRequest, currentUser, AuditActionType.Submitted, "Billing request submitted to Accounts.", now, beforeStatus, BillingRequestStatus.AccountsReview.ToString());
        AddAuditLog(billingRequest, accountsUser, AuditActionType.Assigned, "Billing request assigned to Accounts.", now);

        await SaveWorkflowAsync(cancellationToken);
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
        var settings = await _settingsService.GetAsync(cancellationToken);
        if (billingRequest.Status == BillingRequestStatus.AccountsReview)
        {
            EnsureAccountsOrAdmin(currentUser);
            if (billingRequest.TotalAmount <= settings.ManagerApprovalThreshold)
            {
                billingRequest.Status = BillingRequestStatus.InvoiceGenerated;
                billingRequest.ApprovedAtUtc = now;
                billingRequest.UpdatedAtUtc = now;
                billingRequest.AccountsReviewedByUserId = currentUser.Id;
                billingRequest.LastWorkflowActionAtUtc = now;
                ClearAssignment(billingRequest);
                AddAuditLog(billingRequest, currentUser, AuditActionType.Approved, "Accounts approved billing request.", now, BillingRequestStatus.AccountsReview.ToString(), BillingRequestStatus.InvoiceGenerated.ToString());
                AddOptionalComment(billingRequest.Id, currentUser.Id, request.Comment, now);
                await CreateInvoiceAsync(billingRequest, currentUser, now, settings, cancellationToken);
            }
            else
            {
                var managerUser = await GetFirstActiveUserAsync(RoleName.Manager, cancellationToken);
                billingRequest.Status = BillingRequestStatus.ManagerApproval;
                billingRequest.AssignedToUserId = managerUser.Id;
                billingRequest.AssignedQueue = WorkflowQueue.Manager;
                billingRequest.AssignedAtUtc = now;
                billingRequest.AccountsReviewedByUserId = currentUser.Id;
                billingRequest.UpdatedAtUtc = now;
                billingRequest.LastWorkflowActionAtUtc = now;
                AddAuditLog(billingRequest, currentUser, AuditActionType.Approved, "Accounts approved billing request for manager review.", now, BillingRequestStatus.AccountsReview.ToString(), BillingRequestStatus.ManagerApproval.ToString());
                AddAuditLog(billingRequest, managerUser, AuditActionType.Assigned, "Billing request assigned to Management.", now);
                AddOptionalComment(billingRequest.Id, currentUser.Id, request.Comment, now);
            }
        }
        else if (billingRequest.Status == BillingRequestStatus.ManagerApproval)
        {
            EnsureManagerOrAdmin(currentUser);
            billingRequest.Status = BillingRequestStatus.InvoiceGenerated;
            billingRequest.ApprovedAtUtc = now;
            billingRequest.UpdatedAtUtc = now;
            billingRequest.ManagerReviewedByUserId = currentUser.Id;
            billingRequest.LastWorkflowActionAtUtc = now;
            ClearAssignment(billingRequest);
            AddAuditLog(billingRequest, currentUser, AuditActionType.Approved, "Management approved billing request.", now, BillingRequestStatus.ManagerApproval.ToString(), BillingRequestStatus.InvoiceGenerated.ToString());
            AddOptionalComment(billingRequest.Id, currentUser.Id, request.Comment, now);
            await CreateInvoiceAsync(billingRequest, currentUser, now, settings, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Billing request is not waiting for approval.");
        }

        await SaveWorkflowAsync(cancellationToken);
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

        var beforeStatus = billingRequest.Status.ToString();
        var now = DateTime.UtcNow;
        billingRequest.Status = BillingRequestStatus.Rejected;
        AssignToSales(billingRequest, now);
        billingRequest.RejectedAtUtc = now;
        billingRequest.UpdatedAtUtc = now;
        billingRequest.LastWorkflowActionAtUtc = now;
        AddAuditLog(billingRequest, currentUser, AuditActionType.Rejected, request.Reason.Trim(), now, beforeStatus, BillingRequestStatus.Rejected.ToString());
        AddOptionalComment(billingRequest.Id, currentUser.Id, request.Reason, now);

        await SaveWorkflowAsync(cancellationToken);
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
        AddAuditLog(billingRequest, currentUser, AuditActionType.Commented, "Comment added.", now);

        await SaveWorkflowAsync(cancellationToken);
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

    private IQueryable<BillingRequest> BuildListQuery(BillingRequestQuery query, CurrentUser currentUser)
    {
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

        if (query.Queue is not null)
        {
            requests = requests.Where(x => x.AssignedQueue == query.Queue);
        }

        if (query.AssignedToMe)
        {
            requests = requests.Where(x => x.AssignedToUserId == currentUser.Id);
        }

        if (query.CreatedByMe)
        {
            requests = requests.Where(x => x.CreatedByUserId == currentUser.Id);
        }

        var search = PagingQueryGuard.Search(query.Search);
        if (search is not null)
        {
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

        return requests;
    }

    private static IQueryable<BillingRequest> ApplySort(IQueryable<BillingRequest> query, string? sortBy, string? sortDirection)
    {
        var descending = PagingQueryGuard.Descending(sortDirection);
        var sort = PagingQueryGuard.SortBy(sortBy, "createdAtUtc", "createdAtUtc", "updatedAtUtc", "amount", "status", "clientName", "requestNumber");

        return sort.ToLowerInvariant() switch
        {
            "requestnumber" => descending ? query.OrderByDescending(x => x.RequestNumber) : query.OrderBy(x => x.RequestNumber),
            "updatedatutc" => descending ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.UpdatedAtUtc),
            "amount" => descending ? query.OrderByDescending(x => x.TotalAmount) : query.OrderBy(x => x.TotalAmount),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "clientname" => descending ? query.OrderByDescending(x => x.Customer.Name) : query.OrderBy(x => x.Customer.Name),
            _ => descending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc)
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

    private void SetLineItemsAndAmounts(BillingRequest billingRequest, IReadOnlyList<CreateBillingRequestLineItemDto> lineItems, decimal vatPercentage)
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
        billingRequest.VatAmount = Math.Round(billingRequest.SubtotalAmount * (vatPercentage / 100m), 2);
        billingRequest.TotalAmount = billingRequest.SubtotalAmount + billingRequest.VatAmount;
    }

    private async Task CreateInvoiceAsync(
        BillingRequest billingRequest,
        CurrentUser currentUser,
        DateTime now,
        SystemSettingsDto settings,
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
            VatPercentage = settings.VatPercentage,
            VatAmount = billingRequest.VatAmount,
            TotalAmount = billingRequest.TotalAmount,
            Status = InvoiceStatus.Issued,
            IssuedAtUtc = now,
            DueDays = settings.InvoiceDueDays,
            DueAtUtc = now.AddDays(settings.InvoiceDueDays)
        };

        _dbContext.Invoices.Add(invoice);
        AddAuditLog(billingRequest, currentUser, AuditActionType.InvoiceGenerated, "Invoice generated.", now);
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

    private async Task<WorkflowActor> GetFirstActiveUserAsync(RoleName role, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .Where(x => x.Role == role && x.IsActive)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new WorkflowActor(x.Id, x.FullName))
            .FirstAsync(cancellationToken);
    }

    private async Task EnsureActiveCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.Id == customerId)
            .Select(x => new { x.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException("Client was not found.");
        }

        if (customer.Status != ClientStatus.Active)
        {
            throw new InvalidOperationException("Billing requests can only use active clients.");
        }
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

    private void AssignToSales(BillingRequest billingRequest, DateTime now)
    {
        billingRequest.AssignedToUserId = billingRequest.CreatedByUserId;
        billingRequest.AssignedQueue = WorkflowQueue.Sales;
        billingRequest.AssignedAtUtc = now;
    }

    private static void ClearAssignment(BillingRequest billingRequest)
    {
        billingRequest.AssignedToUserId = null;
        billingRequest.AssignedQueue = WorkflowQueue.None;
        billingRequest.AssignedAtUtc = null;
    }

    private void AddAuditLog(
        BillingRequest billingRequest,
        CurrentUser currentUser,
        AuditActionType actionType,
        string message,
        DateTime now,
        string? beforeStatus = null,
        string? afterStatus = null)
    {
        AddAuditLog(
            billingRequest,
            new WorkflowActor(currentUser.Id, currentUser.FullName),
            actionType,
            message,
            now,
            beforeStatus,
            afterStatus);
    }

    private void AddAuditLog(
        BillingRequest billingRequest,
        WorkflowActor actor,
        AuditActionType actionType,
        string message,
        DateTime now,
        string? beforeStatus = null,
        string? afterStatus = null)
    {
        _auditWriter.Add(new WorkflowAuditEntry(
            billingRequest.Id,
            "BillingRequest",
            billingRequest.Id,
            billingRequest.RequestNumber,
            actor.Id,
            actor.FullName,
            actionType,
            message,
            now,
            beforeStatus,
            afterStatus));
    }

    private async Task SaveWorkflowAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

    private sealed record WorkflowActor(Guid Id, string FullName);
}
