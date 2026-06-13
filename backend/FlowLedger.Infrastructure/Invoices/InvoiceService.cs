using FlowLedger.Application.Audit;
using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;
using FlowLedger.Application.Invoices;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Common;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Invoices;

public sealed class InvoiceService : IInvoiceService
{
    private const int MaxExportRows = 5000;

    private readonly FlowLedgerDbContext _dbContext;
    private readonly IWorkflowAuditWriter _auditWriter;
    private readonly ICsvExportService _csvExportService;

    public InvoiceService(FlowLedgerDbContext dbContext, IWorkflowAuditWriter auditWriter, ICsvExportService csvExportService)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _csvExportService = csvExportService;
    }

    public async Task<PagedResult<InvoiceListItemDto>> GetAsync(InvoiceQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var page = PagingQueryGuard.Page(query.Page);
        var pageSize = PagingQueryGuard.PageSize(query.PageSize);
        var invoices = BuildListQuery(query, currentUser);

        var totalCount = await invoices.CountAsync(cancellationToken);
        var items = await ApplySort(invoices, query.SortBy, query.SortDirection)
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

    public async Task<CsvResult> ExportCsvAsync(InvoiceQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var rows = await ApplySort(BuildListQuery(query, currentUser), query.SortBy, query.SortDirection)
            .Take(MaxExportRows)
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

        return _csvExportService.Export(
            $"invoices-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            rows,
            [
                new("Invoice Number", x => x.InvoiceNumber),
                new("Billing Request Number", x => x.BillingRequestNumber),
                new("Client", x => x.CustomerName),
                new("Status", x => x.Status),
                new("Amount", x => x.TotalAmount),
                new("Issued At UTC", x => x.IssuedAtUtc),
                new("Due At UTC", x => x.DueAtUtc),
                new("Paid At UTC", x => x.PaidAtUtc)
            ]);
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
            invoice.VatPercentage,
            invoice.VatAmount,
            invoice.TotalAmount,
            invoice.IssuedAtUtc,
            invoice.DueDays,
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
        invoice.BillingRequest.AssignedQueue = WorkflowQueue.None;
        invoice.BillingRequest.AssignedToUserId = null;
        invoice.BillingRequest.AssignedAtUtc = null;
        invoice.BillingRequest.LastWorkflowActionAtUtc = now;
        invoice.BillingRequest.UpdatedAtUtc = now;
        _auditWriter.Add(new WorkflowAuditEntry(
            invoice.BillingRequestId,
            "Invoice",
            invoice.Id,
            invoice.InvoiceNumber,
            currentUser.Id,
            currentUser.FullName,
            AuditActionType.PaymentMarked,
            "Invoice marked as paid.",
            now,
            InvoiceStatus.Issued.ToString(),
            InvoiceStatus.Paid.ToString()));

        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

    private IQueryable<Invoice> BuildListQuery(InvoiceQuery query, CurrentUser currentUser)
    {
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

        var search = PagingQueryGuard.Search(query.Search);
        if (search is not null)
        {
            invoices = invoices.Where(x =>
                x.InvoiceNumber.Contains(search) ||
                x.BillingRequest.RequestNumber.Contains(search) ||
                x.Customer.Name.Contains(search));
        }

        return invoices;
    }

    private static IQueryable<Invoice> ApplySort(IQueryable<Invoice> query, string? sortBy, string? sortDirection)
    {
        var descending = PagingQueryGuard.Descending(sortDirection);
        var sort = PagingQueryGuard.SortBy(sortBy, "issuedAtUtc", "issuedAtUtc", "createdAtUtc", "dueAtUtc", "paidAtUtc", "amount", "status", "clientName", "invoiceNumber");

        return sort.ToLowerInvariant() switch
        {
            "invoicenumber" => descending ? query.OrderByDescending(x => x.InvoiceNumber) : query.OrderBy(x => x.InvoiceNumber),
            "clientname" => descending ? query.OrderByDescending(x => x.Customer.Name) : query.OrderBy(x => x.Customer.Name),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "amount" => descending ? query.OrderByDescending(x => x.TotalAmount) : query.OrderBy(x => x.TotalAmount),
            "dueatutc" => descending ? query.OrderByDescending(x => x.DueAtUtc) : query.OrderBy(x => x.DueAtUtc),
            "paidatutc" => descending ? query.OrderByDescending(x => x.PaidAtUtc) : query.OrderBy(x => x.PaidAtUtc),
            _ => descending ? query.OrderByDescending(x => x.IssuedAtUtc) : query.OrderBy(x => x.IssuedAtUtc)
        };
    }
}
