using FlowLedger.Application.Common;
using FlowLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Persistence.SeedData;

public sealed class DemoSeedDataRefresher
{
    private readonly FlowLedgerDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DemoSeedDataRefresher(FlowLedgerDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var today = _dateTimeProvider.UtcNow.Date;
        var baseDate = today.AddDays(-28).AddHours(9);

        var requests = await _dbContext.BillingRequests
            .Where(x => x.RequestNumber.StartsWith("BR-2026-"))
            .ToDictionaryAsync(x => x.RequestNumber, cancellationToken);
        var invoices = await _dbContext.Invoices
            .Where(x => x.InvoiceNumber.StartsWith("INV-2026-"))
            .ToDictionaryAsync(x => x.InvoiceNumber, cancellationToken);
        var comments = (await _dbContext.Comments.ToListAsync(cancellationToken))
            .Where(x => x.Id.ToString().StartsWith("dddddddd-dddd-dddd-dddd-", StringComparison.Ordinal))
            .ToList();
        var requestIds = requests.Values.Select(x => x.Id).ToHashSet();
        var auditLogs = await _dbContext.AuditLogs
            .Where(x => x.BillingRequestId != null && requestIds.Contains(x.BillingRequestId.Value))
            .ToListAsync(cancellationToken);
        var notifications = (await _dbContext.Notifications.ToListAsync(cancellationToken))
            .Where(x => x.Id.ToString().StartsWith("ffffffff-ffff-ffff-ffff-", StringComparison.Ordinal))
            .ToList();

        foreach (var request in requests.Values)
        {
            var number = GetTrailingNumber(request.RequestNumber);
            var createdAt = baseDate.AddDays(number);
            request.CreatedAtUtc = createdAt;
            request.SubmittedAtUtc = null;
            request.ApprovedAtUtc = null;
            request.RejectedAtUtc = null;

            switch (request.Status)
            {
                case BillingRequestStatus.Draft:
                    request.UpdatedAtUtc = createdAt.AddHours(3);
                    break;
                case BillingRequestStatus.AccountsReview:
                case BillingRequestStatus.ManagerApproval:
                    request.SubmittedAtUtc = createdAt.AddHours(4);
                    request.UpdatedAtUtc = createdAt.AddDays(1);
                    break;
                case BillingRequestStatus.Rejected:
                    request.SubmittedAtUtc = createdAt.AddHours(4);
                    request.RejectedAtUtc = createdAt.AddDays(2);
                    request.UpdatedAtUtc = request.RejectedAtUtc.Value;
                    break;
                case BillingRequestStatus.InvoiceGenerated:
                    request.SubmittedAtUtc = createdAt.AddHours(4);
                    request.ApprovedAtUtc = createdAt.AddDays(2);
                    request.UpdatedAtUtc = request.ApprovedAtUtc.Value;
                    break;
                case BillingRequestStatus.Paid:
                    request.SubmittedAtUtc = createdAt.AddHours(4);
                    request.ApprovedAtUtc = createdAt.AddDays(2);
                    request.UpdatedAtUtc = createdAt.AddDays(4);
                    break;
                case BillingRequestStatus.Cancelled:
                    request.UpdatedAtUtc = createdAt.AddDays(1);
                    break;
            }

            RefreshAssignmentMetadata(request);
        }

        foreach (var invoice in invoices.Values)
        {
            var number = GetTrailingNumber(invoice.InvoiceNumber);
            var requestNumber = number + 9;
            var request = requests[$"BR-2026-{requestNumber:0000}"];
            var issuedAt = request.ApprovedAtUtc ?? request.UpdatedAtUtc;
            invoice.IssuedAtUtc = issuedAt;
            invoice.DueAtUtc = issuedAt.AddDays(30);
            invoice.PaidAtUtc = invoice.Status == InvoiceStatus.Paid
                ? issuedAt.AddDays(number == 7 ? 7 : number + 1)
                : null;
        }

        foreach (var comment in comments)
        {
            var request = requests.Values.Single(x => x.Id == comment.BillingRequestId);
            comment.CreatedAtUtc = (request.RejectedAtUtc ?? request.SubmittedAtUtc ?? request.CreatedAtUtc).AddHours(1);
        }

        foreach (var auditLog in auditLogs)
        {
            var request = requests.Values.Single(x => x.Id == auditLog.BillingRequestId);
            auditLog.CreatedAtUtc = auditLog.ActionType switch
            {
                AuditActionType.Created => request.CreatedAtUtc.AddMinutes(15),
                AuditActionType.Submitted => (request.SubmittedAtUtc ?? request.CreatedAtUtc).AddMinutes(15),
                AuditActionType.Rejected => (request.RejectedAtUtc ?? request.UpdatedAtUtc).AddMinutes(15),
                AuditActionType.InvoiceGenerated => (request.ApprovedAtUtc ?? request.UpdatedAtUtc).AddMinutes(15),
                AuditActionType.PaymentMarked => request.UpdatedAtUtc.AddMinutes(15),
                _ => request.UpdatedAtUtc.AddMinutes(15)
            };
        }

        AddMissingWorkflowAuditLogs(requests.Values, auditLogs);

        foreach (var notification in notifications)
        {
            notification.CreatedAtUtc = baseDate.AddDays(GetTrailingNumber(notification.Id.ToString()) + 18);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static int GetTrailingNumber(string value)
    {
        var digits = new string(value.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return int.Parse(digits);
    }

    private void AddMissingWorkflowAuditLogs(IEnumerable<Domain.Entities.BillingRequest> requests, ICollection<Domain.Entities.AuditLog> auditLogs)
    {
        foreach (var request in requests)
        {
            if (request.Status is BillingRequestStatus.Draft or BillingRequestStatus.Cancelled)
            {
                continue;
            }

            if (!auditLogs.Any(x => x.BillingRequestId == request.Id && x.ActionType == AuditActionType.Submitted))
            {
                var auditLog = CreateAuditLog(request, AuditActionType.Submitted, "Billing request submitted to Accounts.", request.SubmittedAtUtc ?? request.CreatedAtUtc);
                auditLogs.Add(auditLog);
                _dbContext.AuditLogs.Add(auditLog);
            }

            if (request.AssignedQueue != WorkflowQueue.None &&
                !auditLogs.Any(x => x.BillingRequestId == request.Id && x.ActionType == AuditActionType.Assigned))
            {
                var auditLog = CreateAuditLog(request, AuditActionType.Assigned, $"Billing request assigned to {request.AssignedQueue}.", request.AssignedAtUtc ?? request.UpdatedAtUtc);
                auditLogs.Add(auditLog);
                _dbContext.AuditLogs.Add(auditLog);
            }

            if (request.Status == BillingRequestStatus.Rejected &&
                !auditLogs.Any(x => x.BillingRequestId == request.Id && x.ActionType == AuditActionType.Rejected))
            {
                var auditLog = CreateAuditLog(request, AuditActionType.Rejected, "Billing request rejected for revision.", request.RejectedAtUtc ?? request.UpdatedAtUtc);
                auditLogs.Add(auditLog);
                _dbContext.AuditLogs.Add(auditLog);
            }

            if (request.Status is BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid &&
                !auditLogs.Any(x => x.BillingRequestId == request.Id && x.ActionType == AuditActionType.InvoiceGenerated))
            {
                var auditLog = CreateAuditLog(request, AuditActionType.InvoiceGenerated, "Invoice generated after approval.", request.ApprovedAtUtc ?? request.UpdatedAtUtc);
                auditLogs.Add(auditLog);
                _dbContext.AuditLogs.Add(auditLog);
            }

            if (request.Status == BillingRequestStatus.Paid &&
                !auditLogs.Any(x => x.BillingRequestId == request.Id && x.ActionType == AuditActionType.PaymentMarked))
            {
                var auditLog = CreateAuditLog(request, AuditActionType.PaymentMarked, "Invoice marked as paid.", request.UpdatedAtUtc);
                auditLogs.Add(auditLog);
                _dbContext.AuditLogs.Add(auditLog);
            }
        }
    }

    private static void RefreshAssignmentMetadata(Domain.Entities.BillingRequest request)
    {
        request.AssignedQueue = request.Status switch
        {
            BillingRequestStatus.Draft or BillingRequestStatus.Rejected => WorkflowQueue.Sales,
            BillingRequestStatus.AccountsReview => WorkflowQueue.Accounts,
            BillingRequestStatus.ManagerApproval => WorkflowQueue.Manager,
            _ => WorkflowQueue.None
        };

        request.AssignedToUserId = request.AssignedQueue switch
        {
            WorkflowQueue.Sales => request.CreatedByUserId,
            WorkflowQueue.Accounts => FlowLedgerSeedData.AccountsUserId,
            WorkflowQueue.Manager => FlowLedgerSeedData.ManagerUserId,
            _ => null
        };
        request.AssignedAtUtc = request.AssignedQueue == WorkflowQueue.None
            ? null
            : request.RejectedAtUtc ?? request.SubmittedAtUtc ?? request.CreatedAtUtc;
        request.SubmittedByUserId = request.SubmittedAtUtc is null ? null : request.CreatedByUserId;
        request.AccountsReviewedByUserId = request.Status is BillingRequestStatus.ManagerApproval or BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid
            ? FlowLedgerSeedData.AccountsUserId
            : null;
        request.ManagerReviewedByUserId = request.Status is BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid && request.TotalAmount > 100000m
            ? FlowLedgerSeedData.ManagerUserId
            : null;
        request.LastWorkflowActionAtUtc = request.RejectedAtUtc ?? request.ApprovedAtUtc ?? request.SubmittedAtUtc ?? request.UpdatedAtUtc;
    }

    private static Domain.Entities.AuditLog CreateAuditLog(
        Domain.Entities.BillingRequest request,
        AuditActionType actionType,
        string message,
        DateTime occurredAtUtc)
    {
        return new Domain.Entities.AuditLog
        {
            Id = Guid.Parse($"eeeeeeee-eeee-eeee-{(int)actionType:0000}-{GetTrailingNumber(request.RequestNumber):000000000000}"),
            BillingRequestId = request.Id,
            EntityType = "BillingRequest",
            EntityId = request.Id,
            EntityNumber = request.RequestNumber,
            ActorUserId = ResolveActorUserId(request, actionType),
            ActorDisplayName = FlowLedgerSeedData.Users.Single(x => x.Id == ResolveActorUserId(request, actionType)).FullName,
            ActionType = actionType,
            Message = message,
            CreatedAtUtc = occurredAtUtc.AddMinutes(15)
        };
    }

    private static Guid ResolveActorUserId(Domain.Entities.BillingRequest request, AuditActionType actionType)
    {
        return actionType switch
        {
            AuditActionType.Assigned => request.AssignedToUserId ?? FlowLedgerSeedData.SalesUserId,
            AuditActionType.Rejected => request.AssignedToUserId ?? FlowLedgerSeedData.AccountsUserId,
            AuditActionType.InvoiceGenerated => request.AssignedToUserId ?? FlowLedgerSeedData.AccountsUserId,
            AuditActionType.PaymentMarked => FlowLedgerSeedData.AccountsUserId,
            _ => FlowLedgerSeedData.SalesUserId
        };
    }
}
