using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Invoices;

public sealed record InvoiceQuery(
    InvoiceStatus? Status,
    Guid? CustomerId,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
