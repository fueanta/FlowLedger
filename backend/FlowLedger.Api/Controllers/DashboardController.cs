using FlowLedger.Api.Extensions;
using FlowLedger.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] DashboardQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetSummaryAsync(query, User.ToCurrentUser(), cancellationToken));
    }
}
