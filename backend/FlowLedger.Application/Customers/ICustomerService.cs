namespace FlowLedger.Application.Customers;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAsync(CancellationToken cancellationToken);
}
