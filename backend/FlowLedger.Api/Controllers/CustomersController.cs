using FlowLedger.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowLedger.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetAsync(cancellationToken));
    }
}
