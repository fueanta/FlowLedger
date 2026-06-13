using FlowLedger.Domain.Enums;

namespace FlowLedger.Domain.Entities;

public class BillingRequest
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BillingRequestStatus Status { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public WorkflowQueue AssignedQueue { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public Guid? AccountsReviewedByUserId { get; set; }
    public Guid? ManagerReviewedByUserId { get; set; }
    public DateTime? LastWorkflowActionAtUtc { get; set; }

    public decimal SubtotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<BillingRequestLineItem> LineItems { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
    public Invoice? Invoice { get; set; }
}
