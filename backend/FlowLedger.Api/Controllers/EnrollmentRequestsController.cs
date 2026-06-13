using FlowLedger.Api.Extensions;
using FlowLedger.Application.Common;
using FlowLedger.Application.Enrollment;
using FlowLedger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Route("api/enrollment-requests")]
public sealed class EnrollmentRequestsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentRequestsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Register(RegisterEnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var id = await _enrollmentService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<EnrollmentRequestDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] EnrollmentRequestStatus? status = null,
        [FromQuery] RoleName? requestedRole = null,
        [FromQuery] string? sortBy = "createdAtUtc",
        [FromQuery] string? sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var query = new EnrollmentRequestQuery(page, pageSize, search, status, requestedRole, sortBy, sortDirection);
        return Ok(await _enrollmentService.GetAsync(query, User.ToCurrentUser(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EnrollmentRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _enrollmentService.GetByIdAsync(id, User.ToCurrentUser(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Enrollment request was not found." });
        }
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid id, ApproveEnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _enrollmentService.ApproveAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(Guid id, RejectEnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _enrollmentService.RejectAsync(id, request, User.ToCurrentUser(), cancellationToken));
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
            return NotFound(new { message = "Enrollment request was not found." });
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
