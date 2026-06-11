using FlowLedger.Domain.Enums;

namespace FlowLedger.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid BillingRequestId { get; set; }
    public BillingRequest BillingRequest { get; set; } = null!;

    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;

    public AuditActionType ActionType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
