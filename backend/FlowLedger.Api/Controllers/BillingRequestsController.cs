using FlowLedger.Api.Extensions;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;
using FlowLedger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/billing-requests")]
public sealed class BillingRequestsController : ControllerBase
{
    private readonly IBillingRequestService _billingRequestService;

    public BillingRequestsController(IBillingRequestService billingRequestService)
    {
        _billingRequestService = billingRequestService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BillingRequestListItemDto>>> Get(
        [FromQuery] BillingRequestStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] WorkflowQueue? queue,
        [FromQuery] bool assignedToMe,
        [FromQuery] bool createdByMe,
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? untilDate,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = new BillingRequestQuery(status, customerId, queue, assignedToMe, createdByMe, search, fromDate, untilDate, minAmount, maxAmount, sortBy, sortDirection, page, pageSize);

        try
        {
            return Ok(await _billingRequestService.GetAsync(query, User.ToCurrentUser(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] BillingRequestStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] WorkflowQueue? queue,
        [FromQuery] bool assignedToMe,
        [FromQuery] bool createdByMe,
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? untilDate,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        CancellationToken cancellationToken = default)
    {
        var query = new BillingRequestQuery(status, customerId, queue, assignedToMe, createdByMe, search, fromDate, untilDate, minAmount, maxAmount, sortBy, sortDirection);

        try
        {
            var csv = await _billingRequestService.ExportCsvAsync(query, User.ToCurrentUser(), cancellationToken);
            return File(csv.ToBytes(), CsvResult.ContentType, csv.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillingRequestDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _billingRequestService.GetByIdAsync(id, User.ToCurrentUser(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Billing request was not found." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "SalesOnly")]
    public async Task<ActionResult<object>> Create(CreateBillingRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var id = await _billingRequestService.CreateAsync(request, User.ToCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SalesOnly")]
    public async Task<IActionResult> Update(Guid id, UpdateBillingRequestDto request, CancellationToken cancellationToken)
    {
        return await ExecuteWorkflowActionAsync(
            () => _billingRequestService.UpdateAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = "SalesOnly")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        return await ExecuteWorkflowActionAsync(
            () => _billingRequestService.SubmitAsync(id, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, ApproveBillingRequestDto request, CancellationToken cancellationToken)
    {
        return await ExecuteWorkflowActionAsync(
            () => _billingRequestService.ApproveAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectBillingRequestDto request, CancellationToken cancellationToken)
    {
        return await ExecuteWorkflowActionAsync(
            () => _billingRequestService.RejectAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, AddCommentDto request, CancellationToken cancellationToken)
    {
        return await ExecuteWorkflowActionAsync(
            () => _billingRequestService.AddCommentAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    private async Task<IActionResult> ExecuteWorkflowActionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Billing request was not found." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
