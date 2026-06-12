using FlowLedger.Application.Common;
using FlowLedger.Application.Invoices;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Invoices;

public sealed class InvoiceService : IInvoiceService
{
    private const int MaxPageSize = 100;

    private readonly FlowLedgerDbContext _dbContext;

    public InvoiceService(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<InvoiceListItemDto>> GetAsync(InvoiceQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var invoices = ApplyVisibility(_dbContext.Invoices.AsNoTracking(), currentUser)
            .Include(x => x.Customer)
            .Include(x => x.BillingRequest)
            .AsQueryable();

        if (query.Status is not null)
        {
            invoices = invoices.Where(x => x.Status == query.Status);
        }

        if (query.CustomerId is not null)
        {
            invoices = invoices.Where(x => x.CustomerId == query.CustomerId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            invoices = invoices.Where(x =>
                x.InvoiceNumber.Contains(search) ||
                x.BillingRequest.RequestNumber.Contains(search) ||
                x.Customer.Name.Contains(search));
        }

        var totalCount = await invoices.CountAsync(cancellationToken);
        var items = await invoices
            .OrderByDescending(x => x.IssuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new InvoiceListItemDto(
                x.Id,
                x.InvoiceNumber,
                x.BillingRequest.RequestNumber,
                x.Customer.Name,
                x.Status,
                x.TotalAmount,
                x.IssuedAtUtc,
                x.DueAtUtc,
                x.PaidAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<InvoiceDetailDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var invoice = await ApplyVisibility(_dbContext.Invoices.AsNoTracking(), currentUser)
            .Include(x => x.Customer)
            .Include(x => x.BillingRequest)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException("Invoice was not found.");
        }

        return new InvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.SubtotalAmount,
            invoice.VatAmount,
            invoice.TotalAmount,
            invoice.IssuedAtUtc,
            invoice.DueAtUtc,
            invoice.PaidAtUtc,
            new InvoiceCustomerDto(invoice.Customer.Id, invoice.Customer.Name, invoice.Customer.ContactEmail, invoice.Customer.BillingAddress),
            new InvoiceBillingRequestDto(invoice.BillingRequest.Id, invoice.BillingRequest.RequestNumber, invoice.BillingRequest.Title, invoice.BillingRequest.Status));
    }

    public async Task MarkPaidAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not (RoleName.Accounts or RoleName.Admin))
        {
            throw new UnauthorizedAccessException("Only Accounts or Admin users can mark invoices as paid.");
        }

        var invoice = await _dbContext.Invoices
            .Include(x => x.BillingRequest)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException("Invoice was not found.");
        }

        if (invoice.Status != InvoiceStatus.Issued)
        {
            throw new InvalidOperationException("Only issued invoices can be marked as paid.");
        }

        var now = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAtUtc = now;
        invoice.BillingRequest.Status = BillingRequestStatus.Paid;
        invoice.BillingRequest.UpdatedAtUtc = now;
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            BillingRequestId = invoice.BillingRequestId,
            ActorUserId = currentUser.Id,
            ActionType = AuditActionType.PaymentMarked,
            Message = "Invoice marked as paid.",
            CreatedAtUtc = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Invoice> ApplyVisibility(IQueryable<Invoice> query, CurrentUser currentUser)
    {
        return currentUser.Role switch
        {
            RoleName.Admin => query,
            RoleName.Sales => query.Where(x => x.BillingRequest.CreatedByUserId == currentUser.Id),
            RoleName.Accounts => query,
            RoleName.Manager => query,
            _ => query.Where(x => x.Id == Guid.Empty)
        };
    }
}
