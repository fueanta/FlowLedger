using FlowLedger.Application.Common;
using FlowLedger.Application.Preferences;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Preferences;

public sealed class UserPreferenceService : IUserPreferenceService
{
    private static readonly int[] AllowedDashboardPeriods = [1, 3, 6, 12];
    private static readonly int[] AllowedPageSizes = [10, 25, 50, 100];
    private static readonly string[] SharedLandingPages = ["/app/dashboard", "/app/work-queue", "/app/requests", "/app/invoices", "/app/clients", "/app/settings"];
    private static readonly string[] AdminLandingPages = ["/app/enrollment-requests", "/app/users", "/app/audit-logs"];

    private readonly FlowLedgerDbContext _dbContext;

    public UserPreferenceService(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserPreferenceDto> GetMineAsync(CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var preference = await _dbContext.UserPreferences.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == currentUser.Id, cancellationToken);

        return preference is null ? DefaultFor(currentUser.Role) : ToDto(preference);
    }

    public async Task<UserPreferenceDto> UpdateMineAsync(UpdateUserPreferenceDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        Validate(request, currentUser.Role);

        var now = DateTime.UtcNow;
        var preference = await _dbContext.UserPreferences.SingleOrDefaultAsync(x => x.UserId == currentUser.Id, cancellationToken);
        if (preference is null)
        {
            preference = new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.Id,
                CreatedAtUtc = now
            };
            _dbContext.UserPreferences.Add(preference);
        }

        preference.DefaultDashboardPeriodMonths = request.DefaultDashboardPeriodMonths;
        preference.DefaultLandingPage = request.DefaultLandingPage.Trim();
        preference.RowsPerPage = request.RowsPerPage;
        preference.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(preference);
    }

    private static UserPreferenceDto DefaultFor(RoleName role)
    {
        return role switch
        {
            RoleName.Sales => new UserPreferenceDto(1, "/app/requests", 25),
            RoleName.Admin => new UserPreferenceDto(1, "/app/dashboard", 50),
            _ => new UserPreferenceDto(1, "/app/dashboard", 25)
        };
    }

    private static void Validate(UpdateUserPreferenceDto request, RoleName role)
    {
        if (!AllowedDashboardPeriods.Contains(request.DefaultDashboardPeriodMonths))
        {
            throw new InvalidOperationException("Default dashboard period must be one of 1, 3, 6, or 12 months.");
        }

        if (!AllowedPageSizes.Contains(request.RowsPerPage))
        {
            throw new InvalidOperationException("Rows per page must be one of 10, 25, 50, or 100.");
        }

        var landingPage = request.DefaultLandingPage.Trim();
        var allowedLandingPages = role == RoleName.Admin ? SharedLandingPages.Concat(AdminLandingPages) : SharedLandingPages;
        if (!allowedLandingPages.Contains(landingPage, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Default landing page is not available for this role.");
        }
    }

    private static UserPreferenceDto ToDto(UserPreference preference)
    {
        return new UserPreferenceDto(preference.DefaultDashboardPeriodMonths, preference.DefaultLandingPage, preference.RowsPerPage);
    }
}
