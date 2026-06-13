using FlowLedger.Api.Extensions;
using FlowLedger.Application.Auth;
using FlowLedger.Application.Common;
using FlowLedger.Application.Users;
using FlowLedger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;

    public UsersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] RoleName? role = null,
        [FromQuery] UserStatus? status = null,
        [FromQuery] string? sortBy = "fullName",
        [FromQuery] string? sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = new UserQuery(page, pageSize, search, role, status, sortBy, sortDirection);
        try
        {
            return Ok(await _userAdminService.GetAsync(query, User.ToCurrentUser(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _userAdminService.GetByIdAsync(id, User.ToCurrentUser(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User was not found." });
        }
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, UpdateUserRoleDto request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _userAdminService.UpdateRoleAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _userAdminService.ActivateAsync(id, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _userAdminService.DeactivateAsync(id, User.ToCurrentUser(), cancellationToken));
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User was not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
