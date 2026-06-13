using FlowLedger.Application.Common;

namespace FlowLedger.Application.Preferences;

public sealed record UserPreferenceDto(
    int DefaultDashboardPeriodMonths,
    string DefaultLandingPage,
    int RowsPerPage);

public sealed record UpdateUserPreferenceDto(
    int DefaultDashboardPeriodMonths,
    string DefaultLandingPage,
    int RowsPerPage);

public interface IUserPreferenceService
{
    Task<UserPreferenceDto> GetMineAsync(CurrentUser currentUser, CancellationToken cancellationToken);
    Task<UserPreferenceDto> UpdateMineAsync(UpdateUserPreferenceDto request, CurrentUser currentUser, CancellationToken cancellationToken);
}
