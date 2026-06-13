using FlowLedger.Api.Extensions;
using FlowLedger.Application.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settingsService;

    public SettingsController(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<SystemSettingsDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _settingsService.GetAsync(cancellationToken));
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(UpdateSystemSettingsDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _settingsService.UpdateAsync(request, User.ToCurrentUser(), cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
