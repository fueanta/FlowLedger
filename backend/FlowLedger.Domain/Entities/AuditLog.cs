using FlowLedger.Domain.Enums;

namespace FlowLedger.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? BillingRequestId { get; set; }
    public BillingRequest? BillingRequest { get; set; }

    public string EntityType { get; set; } = "BillingRequest";
    public Guid EntityId { get; set; }
    public string? EntityNumber { get; set; }

    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public string ActorDisplayName { get; set; } = string.Empty;

    public AuditActionType ActionType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? BeforeStatus { get; set; }
    public string? AfterStatus { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
