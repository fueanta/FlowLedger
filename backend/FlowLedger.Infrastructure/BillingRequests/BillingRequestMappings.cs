using FlowLedger.Application.BillingRequests;
using FlowLedger.Domain.Entities;

namespace FlowLedger.Infrastructure.BillingRequests;

internal static class BillingRequestMappings
{
    public static BillingRequestDetailDto ToDetailDto(this BillingRequest request, IReadOnlyList<string> availableActions)
    {
        return new BillingRequestDetailDto(
            request.Id,
            request.RequestNumber,
            request.Title,
            request.Description,
            request.Status,
            new CustomerSummaryDto(request.Customer.Id, request.Customer.Name, request.Customer.ContactEmail),
            request.CreatedByUser.ToSummaryDto(),
            request.AssignedToUser?.ToSummaryDto(),
            request.AssignedQueue,
            request.AssignedAtUtc,
            request.LastWorkflowActionAtUtc,
            request.SubtotalAmount,
            request.VatAmount,
            request.TotalAmount,
            request.SubmittedAtUtc,
            request.ApprovedAtUtc,
            request.RejectedAtUtc,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.LineItems
                .OrderBy(x => x.Description)
                .Select(x => new BillingRequestLineItemDto(x.Id, x.Description, x.Quantity, x.UnitPrice, x.LineTotal))
                .ToList(),
            request.Comments
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new CommentDto(x.Id, x.AuthorUser.ToSummaryDto(), x.Body, x.CreatedAtUtc))
                .ToList(),
            request.AuditLogs
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new AuditLogDto(x.Id, x.ActorUser.ToSummaryDto(), x.ActionType, x.Message, x.CreatedAtUtc))
                .ToList(),
            request.Invoice is null
                ? null
                : new BillingRequestInvoiceDto(
                    request.Invoice.Id,
                    request.Invoice.InvoiceNumber,
                    request.Invoice.Status,
                    request.Invoice.TotalAmount),
            availableActions);
    }

    public static UserSummaryDto ToSummaryDto(this User user)
    {
        return new UserSummaryDto(user.Id, user.FullName, user.Email, user.Role);
    }
}
