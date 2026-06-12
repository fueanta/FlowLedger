using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Tests;

internal static class TestAuthSeedData
{
    public static readonly Guid TestSalesUserId = Guid.Parse("12121212-1212-1212-1212-121212121212");
    public const string TestSalesEmail = "auth-test-sales@flowledger.local";
    public const string TestSalesPassword = "test-only-sales-password";

    public static User TestSalesUser(string passwordHash, string passwordSalt)
    {
        return new User
        {
            Id = TestSalesUserId,
            FullName = "Test Sales User",
            Email = TestSalesEmail,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = RoleName.Sales,
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc)
        };
    }
}
