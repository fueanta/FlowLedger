namespace FlowLedger.Domain.Entities;

public class UserPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int DefaultDashboardPeriodMonths { get; set; }
    public string DefaultLandingPage { get; set; } = string.Empty;
    public int RowsPerPage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
