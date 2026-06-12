using FlowLedger.Application.Common;

namespace FlowLedger.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DashboardQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
}
