using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Audit;

public sealed record WorkflowAuditEntry(
    Guid? BillingRequestId,
    string EntityType,
    Guid EntityId,
    string? EntityNumber,
    Guid ActorUserId,
    string ActorDisplayName,
    AuditActionType ActionType,
    string Message,
    DateTime CreatedAtUtc,
    string? BeforeStatus = null,
    string? AfterStatus = null);

public interface IWorkflowAuditWriter
{
    void Add(WorkflowAuditEntry entry);
}
