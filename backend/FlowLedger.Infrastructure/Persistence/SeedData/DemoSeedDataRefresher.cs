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

        await UpsertDashboardDensityRowsAsync(today, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertDashboardDensityRowsAsync(DateTime today, CancellationToken cancellationToken)
    {
        var specs = DashboardDensitySpecs();
        var requests = await _dbContext.BillingRequests
            .Include(x => x.LineItems)
            .Include(x => x.Invoice)
            .Where(x => x.RequestNumber.StartsWith("BR-DASH-"))
            .ToDictionaryAsync(x => x.RequestNumber, cancellationToken);
        var auditLogs = await _dbContext.AuditLogs
            .Where(x => x.EntityNumber != null && x.EntityNumber.StartsWith("BR-DASH-"))
            .ToListAsync(cancellationToken);

        foreach (var spec in specs)
        {
            var createdAt = today.AddDays(-spec.AgeDays).AddHours(10);
            var submittedAt = createdAt.AddHours(2);
            var approvedAt = spec.Status is BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid
                ? createdAt.AddDays(1)
                : (DateTime?)null;
            var updatedAt = spec.Status == BillingRequestStatus.Paid
                ? approvedAt!.Value.AddDays(2)
                : approvedAt ?? createdAt.AddDays(1);
            var subtotal = Math.Round(spec.TotalAmount / 1.15m, 2);
            var vat = spec.TotalAmount - subtotal;

            if (!requests.TryGetValue(spec.RequestNumber, out var request))
            {
                request = new Domain.Entities.BillingRequest
                {
                    Id = spec.RequestId,
                    RequestNumber = spec.RequestNumber,
                    CreatedByUserId = FlowLedgerSeedData.SalesUserId,
                    CustomerId = spec.CustomerId
                };
                _dbContext.BillingRequests.Add(request);
                requests[spec.RequestNumber] = request;
            }

            request.Title = spec.Title;
            request.Description = $"{spec.Title} for rolling dashboard demo reporting.";
            request.Status = spec.Status;
            request.AssignedQueue = WorkflowQueue.None;
            request.AssignedToUserId = null;
            request.AssignedAtUtc = null;
            request.SubmittedByUserId = FlowLedgerSeedData.SalesUserId;
            request.AccountsReviewedByUserId = FlowLedgerSeedData.AccountsUserId;
            request.ManagerReviewedByUserId = spec.TotalAmount > 100000m ? FlowLedgerSeedData.ManagerUserId : null;
            request.LastWorkflowActionAtUtc = updatedAt;
            request.SubtotalAmount = subtotal;
            request.VatAmount = vat;
            request.TotalAmount = spec.TotalAmount;
            request.SubmittedAtUtc = submittedAt;
            request.ApprovedAtUtc = approvedAt;
            request.RejectedAtUtc = null;
            request.CreatedAtUtc = createdAt;
            request.UpdatedAtUtc = updatedAt;

            var lineItem = request.LineItems.SingleOrDefault();
            if (lineItem is null)
            {
                lineItem = new Domain.Entities.BillingRequestLineItem
                {
                    Id = spec.LineItemId,
                    BillingRequestId = spec.RequestId
                };
                request.LineItems.Add(lineItem);
                _dbContext.BillingRequestLineItems.Add(lineItem);
            }

            lineItem.Description = $"{spec.Title} line item";
            lineItem.Quantity = 1;
            lineItem.UnitPrice = subtotal;
            lineItem.LineTotal = subtotal;

            if (spec.Status is BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid)
            {
                var invoice = request.Invoice;
                if (invoice is null)
                {
                    invoice = new Domain.Entities.Invoice
                    {
                        Id = spec.InvoiceId!.Value,
                        InvoiceNumber = spec.InvoiceNumber!,
                        BillingRequestId = spec.RequestId,
                        CustomerId = spec.CustomerId
                    };
                    _dbContext.Invoices.Add(invoice);
                    request.Invoice = invoice;
                }

                invoice.SubtotalAmount = subtotal;
                invoice.VatPercentage = 15m;
                invoice.VatAmount = vat;
                invoice.TotalAmount = spec.TotalAmount;
                invoice.Status = spec.Status == BillingRequestStatus.Paid ? InvoiceStatus.Paid : InvoiceStatus.Issued;
                invoice.IssuedAtUtc = approvedAt!.Value;
                invoice.DueDays = 30;
                invoice.DueAtUtc = approvedAt.Value.AddDays(30);
                invoice.PaidAtUtc = spec.Status == BillingRequestStatus.Paid ? updatedAt : null;
            }

            UpsertAuditLog(auditLogs, spec.AuditBase, request, AuditActionType.Created, FlowLedgerSeedData.SalesUserId, "Billing request created.", createdAt.AddMinutes(10));
            UpsertAuditLog(auditLogs, spec.AuditBase + 1, request, AuditActionType.Submitted, FlowLedgerSeedData.SalesUserId, "Billing request submitted to Accounts.", submittedAt.AddMinutes(10));

            if (spec.Status is BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid)
            {
                UpsertAuditLog(auditLogs, spec.AuditBase + 2, request, AuditActionType.InvoiceGenerated, FlowLedgerSeedData.AccountsUserId, "Invoice generated after approval.", approvedAt!.Value.AddMinutes(10));
            }

            if (spec.Status == BillingRequestStatus.Paid)
            {
                UpsertAuditLog(auditLogs, spec.AuditBase + 3, request, AuditActionType.PaymentMarked, FlowLedgerSeedData.AccountsUserId, "Invoice marked as paid.", updatedAt.AddMinutes(10));
            }
        }
    }

    private void UpsertAuditLog(
        ICollection<Domain.Entities.AuditLog> auditLogs,
        int number,
        Domain.Entities.BillingRequest request,
        AuditActionType actionType,
        Guid actorUserId,
        string message,
        DateTime createdAtUtc)
    {
        var id = Guid.Parse($"edededed-eded-eded-eded-{number:000000000000}");
        var auditLog = auditLogs.SingleOrDefault(x => x.Id == id);
        if (auditLog is null)
        {
            auditLog = new Domain.Entities.AuditLog
            {
                Id = id,
                BillingRequestId = request.Id,
                EntityId = request.Id
            };
            auditLogs.Add(auditLog);
            _dbContext.AuditLogs.Add(auditLog);
        }

        auditLog.BillingRequestId = request.Id;
        auditLog.EntityType = "BillingRequest";
        auditLog.EntityId = request.Id;
        auditLog.EntityNumber = request.RequestNumber;
        auditLog.ActorUserId = actorUserId;
        auditLog.ActorDisplayName = FlowLedgerSeedData.Users.Single(x => x.Id == actorUserId).FullName;
        auditLog.ActionType = actionType;
        auditLog.Message = message;
        auditLog.CreatedAtUtc = createdAtUtc;
    }

    private static IReadOnlyList<DashboardDensitySpec> DashboardDensitySpecs()
    {
        return
        [
            new(1, 5, FlowLedgerSeedData.FiberRetailCustomerId, "Recent retail renewal", BillingRequestStatus.InvoiceGenerated, 62000m),
            new(2, 12, FlowLedgerSeedData.MetroLogisticsCustomerId, "Recent freight settlement", BillingRequestStatus.Paid, 81000m),
            new(3, 50, FlowLedgerSeedData.NorthstarCustomerId, "Quarterly service closeout", BillingRequestStatus.InvoiceGenerated, 74000m),
            new(4, 75, FlowLedgerSeedData.GreenlineCustomerId, "Quarterly distribution billing", BillingRequestStatus.Paid, 56000m),
            new(5, 135, FlowLedgerSeedData.BluePeakCustomerId, "Midyear platform expansion", BillingRequestStatus.InvoiceGenerated, 132000m),
            new(6, 170, FlowLedgerSeedData.EasternTradingCustomerId, "Midyear supply billing", BillingRequestStatus.Paid, 116000m),
            new(7, 255, FlowLedgerSeedData.FiberRetailCustomerId, "Annual service true-up", BillingRequestStatus.InvoiceGenerated, 98000m),
            new(8, 335, FlowLedgerSeedData.MetroLogisticsCustomerId, "Legacy freight adjustment", BillingRequestStatus.Paid, 67000m)
        ];
    }

    private sealed record DashboardDensitySpec(
        int Number,
        int AgeDays,
        Guid CustomerId,
        string Title,
        BillingRequestStatus Status,
        decimal TotalAmount)
    {
        public Guid RequestId => Guid.Parse($"abababab-abab-abab-abab-{Number:000000000000}");
        public Guid LineItemId => Guid.Parse($"babababa-baba-baba-baba-{Number:000000000000}");
        public Guid? InvoiceId => Status is BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid
            ? Guid.Parse($"cacacaca-caca-caca-caca-{Number:000000000000}")
            : null;
        public string RequestNumber => $"BR-DASH-{Number:0000}";
        public string? InvoiceNumber => InvoiceId is null ? null : $"INV-DASH-{Number:0000}";
        public int AuditBase => Number * 10;
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
