using FlowLedger.Api.Extensions;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Application.WorkQueue;
using FlowLedger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/work-queue")]
public sealed class WorkQueueController : ControllerBase
{
    private readonly IWorkQueueService _workQueueService;

    public WorkQueueController(IWorkQueueService workQueueService)
    {
        _workQueueService = workQueueService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BillingRequestListItemDto>>> Get(
        [FromQuery] WorkflowQueue? queue,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = new WorkQueueQuery(queue, search, sortBy, sortDirection, page, pageSize);
        try
        {
            return Ok(await _workQueueService.GetAsync(query, User.ToCurrentUser(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
