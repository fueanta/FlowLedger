using FlowLedger.Api.Extensions;
using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;
using FlowLedger.Application.Customers;
using FlowLedger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/clients")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] ClientStatus? status = null,
        [FromQuery] string? sortBy = "companyName",
        [FromQuery] string? sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = new ClientQuery(page, pageSize, search, status, sortBy, sortDirection);
        try
        {
            return Ok(await _customerService.GetAsync(query, User.ToCurrentUser(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search = null,
        [FromQuery] ClientStatus? status = null,
        [FromQuery] string? sortBy = "companyName",
        [FromQuery] string? sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = new ClientQuery(Search: search, Status: status, SortBy: sortBy, SortDirection: sortDirection);
        try
        {
            var csv = await _customerService.ExportCsvAsync(query, User.ToCurrentUser(), cancellationToken);
            return File(csv.ToBytes(), CsvResult.ContentType, csv.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _customerService.GetByIdAsync(id, User.ToCurrentUser(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Client was not found." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create(CreateClientDto request, CancellationToken cancellationToken)
    {
        try
        {
            var id = await _customerService.CreateAsync(request, User.ToCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateClientDto request, CancellationToken cancellationToken)
    {
        return await ExecuteClientActionAsync(() => _customerService.UpdateAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        return await ExecuteClientActionAsync(() => _customerService.ArchiveAsync(id, User.ToCurrentUser(), cancellationToken));
    }

    private async Task<IActionResult> ExecuteClientActionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Client was not found." });
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

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/customers")]
public sealed class LegacyCustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public LegacyCustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetAsync(cancellationToken));
    }
}
