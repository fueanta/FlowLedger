using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Invoices;

public sealed record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    string BillingRequestNumber,
    string CustomerName,
    InvoiceStatus Status,
    decimal TotalAmount,
    DateTime IssuedAtUtc,
    DateTime DueAtUtc,
    DateTime? PaidAtUtc);

public sealed record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    InvoiceStatus Status,
    decimal SubtotalAmount,
    decimal VatPercentage,
    decimal VatAmount,
    decimal TotalAmount,
    DateTime IssuedAtUtc,
    int DueDays,
    DateTime DueAtUtc,
    DateTime? PaidAtUtc,
    InvoiceCustomerDto Customer,
    InvoiceBillingRequestDto BillingRequest);

public sealed record InvoiceCustomerDto(Guid Id, string Name, string ContactEmail, string BillingAddress);

public sealed record InvoiceBillingRequestDto(Guid Id, string RequestNumber, string Title, BillingRequestStatus Status);
