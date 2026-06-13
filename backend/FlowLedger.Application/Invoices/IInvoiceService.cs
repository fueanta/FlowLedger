using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;

namespace FlowLedger.Application.Invoices;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> GetAsync(InvoiceQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<CsvResult> ExportCsvAsync(InvoiceQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<InvoiceDetailDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task MarkPaidAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
}

public interface IInvoicePdfService
{
    Task<InvoicePdfResult> GenerateAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
}

public sealed record InvoicePdfResult(string FileName, byte[] Content)
{
    public const string ContentType = "application/pdf";
}
