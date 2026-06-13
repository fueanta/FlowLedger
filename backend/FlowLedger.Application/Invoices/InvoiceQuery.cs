using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Invoices;

public sealed record InvoiceQuery(
    InvoiceStatus? Status,
    Guid? CustomerId,
    string? Search = null,
    string? SortBy = "issuedAtUtc",
    string? SortDirection = "desc",
    int Page = 1,
    int PageSize = 25);
