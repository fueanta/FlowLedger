using FlowLedger.Api.Extensions;
using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;
using FlowLedger.Application.Invoices;
using FlowLedger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoicePdfService _invoicePdfService;

    public InvoicesController(IInvoiceService invoiceService, IInvoicePdfService invoicePdfService)
    {
        _invoiceService = invoiceService;
        _invoicePdfService = invoicePdfService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> Get(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] Guid? customerId,
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
        var query = new InvoiceQuery(status, customerId, search, fromDate, untilDate, minAmount, maxAmount, sortBy, sortDirection, page, pageSize);
        try
        {
            return Ok(await _invoiceService.GetAsync(query, User.ToCurrentUser(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? untilDate,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        CancellationToken cancellationToken = default)
    {
        var query = new InvoiceQuery(status, customerId, search, fromDate, untilDate, minAmount, maxAmount, sortBy, sortDirection);
        try
        {
            var csv = await _invoiceService.ExportCsvAsync(query, User.ToCurrentUser(), cancellationToken);
            return File(csv.ToBytes(), CsvResult.ContentType, csv.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _invoiceService.GetByIdAsync(id, User.ToCurrentUser(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Invoice was not found." });
        }
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var pdf = await _invoicePdfService.GenerateAsync(id, User.ToCurrentUser(), cancellationToken);
            return File(pdf.Content, InvoicePdfResult.ContentType, pdf.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Invoice was not found." });
        }
    }

    [HttpPost("{id:guid}/mark-paid")]
    [Authorize(Policy = "AccountsOnly")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _invoiceService.MarkPaidAsync(id, User.ToCurrentUser(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Invoice was not found." });
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
