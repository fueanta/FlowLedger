namespace FlowLedger.Domain.Entities;

using FlowLedger.Domain.Enums;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public ClientStatus Status { get; set; } = ClientStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? ArchivedByUserId { get; set; }
}
