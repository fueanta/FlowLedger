using FlowLedger.Application.Customers;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Customers;

public sealed class CustomerService : ICustomerService
{
    private readonly FlowLedgerDbContext _dbContext;

    public CustomerService(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CustomerDto(x.Id, x.Name, x.ContactEmail, x.Phone, x.BillingAddress))
            .ToListAsync(cancellationToken);
    }
}
