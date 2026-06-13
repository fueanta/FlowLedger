using FlowLedger.Application.Audit;
using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence;

namespace FlowLedger.Infrastructure.Audit;

public sealed class WorkflowAuditWriter : IWorkflowAuditWriter
{
    private readonly FlowLedgerDbContext _dbContext;

    public WorkflowAuditWriter(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(WorkflowAuditEntry entry)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            BillingRequestId = entry.BillingRequestId,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            EntityNumber = entry.EntityNumber,
            ActorUserId = entry.ActorUserId,
            ActorDisplayName = entry.ActorDisplayName,
            ActionType = entry.ActionType,
            Message = entry.Message,
            BeforeStatus = entry.BeforeStatus,
            AfterStatus = entry.AfterStatus,
            CreatedAtUtc = entry.CreatedAtUtc
        });
    }
}
