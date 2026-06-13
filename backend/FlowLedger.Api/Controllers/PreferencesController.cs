using FlowLedger.Api.Extensions;
using FlowLedger.Application.Preferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/preferences/me")]
public sealed class PreferencesController : ControllerBase
{
    private readonly IUserPreferenceService _preferenceService;

    public PreferencesController(IUserPreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    [HttpGet]
    public async Task<ActionResult<UserPreferenceDto>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _preferenceService.GetMineAsync(User.ToCurrentUser(), cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult<UserPreferenceDto>> UpdateMine(UpdateUserPreferenceDto request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _preferenceService.UpdateMineAsync(request, User.ToCurrentUser(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
