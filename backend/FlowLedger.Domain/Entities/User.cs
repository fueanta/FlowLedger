using FlowLedger.Domain.Enums;

namespace FlowLedger.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public RoleName Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }
    public Guid? DeactivatedByUserId { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public Guid? EnrollmentRequestId { get; set; }
}
