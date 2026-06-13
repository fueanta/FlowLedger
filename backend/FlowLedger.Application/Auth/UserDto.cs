using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Auth;

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    RoleName Role,
    UserStatus Status,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastLoginAtUtc);
