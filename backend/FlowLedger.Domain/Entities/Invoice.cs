using FlowLedger.Domain.Enums;

namespace FlowLedger.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid BillingRequestId { get; set; }
    public BillingRequest BillingRequest { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal SubtotalAmount { get; set; }
    public decimal VatPercentage { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; }

    public DateTime IssuedAtUtc { get; set; }
    public int DueDays { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
