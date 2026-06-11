namespace FlowLedger.Domain.Entities;

public class BillingRequestLineItem
{
    public Guid Id { get; set; }
    public Guid BillingRequestId { get; set; }
    public BillingRequest BillingRequest { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
