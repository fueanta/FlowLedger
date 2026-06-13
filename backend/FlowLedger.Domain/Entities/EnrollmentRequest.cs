using FlowLedger.Domain.Enums;

namespace FlowLedger.Domain.Entities;

public class EnrollmentRequest
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RoleName RequestedRole { get; set; } = RoleName.Sales;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public EnrollmentRequestStatus Status { get; set; } = EnrollmentRequestStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
