using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.BillingRequests;

public sealed record BillingRequestListItemDto(
    Guid Id,
    string RequestNumber,
    string Title,
    string CustomerName,
    BillingRequestStatus Status,
    WorkflowQueue AssignedQueue,
    DateTime? AssignedAtUtc,
    DateTime? LastWorkflowActionAtUtc,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BillingRequestDetailDto(
    Guid Id,
    string RequestNumber,
    string Title,
    string Description,
    BillingRequestStatus Status,
    CustomerSummaryDto Customer,
    UserSummaryDto CreatedBy,
    UserSummaryDto? AssignedTo,
    WorkflowQueue AssignedQueue,
    DateTime? AssignedAtUtc,
    DateTime? LastWorkflowActionAtUtc,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    DateTime? SubmittedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? RejectedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<BillingRequestLineItemDto> LineItems,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<AuditLogDto> AuditLogs,
    BillingRequestInvoiceDto? Invoice,
    IReadOnlyList<string> AvailableActions);

public sealed record CustomerSummaryDto(Guid Id, string Name, string ContactEmail);

public sealed record UserSummaryDto(Guid Id, string FullName, string Email, RoleName Role);

public sealed record BillingRequestLineItemDto(Guid Id, string Description, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record CommentDto(Guid Id, UserSummaryDto Author, string Body, DateTime CreatedAtUtc);

public sealed record AuditLogDto(Guid Id, UserSummaryDto Actor, AuditActionType ActionType, string Message, DateTime CreatedAtUtc);

public sealed record BillingRequestInvoiceDto(Guid Id, string InvoiceNumber, InvoiceStatus Status, decimal TotalAmount);
