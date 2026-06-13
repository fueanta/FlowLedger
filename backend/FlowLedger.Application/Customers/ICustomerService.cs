using FlowLedger.Application.Common;

namespace FlowLedger.Application.Customers;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAsync(CancellationToken cancellationToken);
    Task<PagedResult<CustomerDto>> GetAsync(ClientQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<CustomerDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(CreateClientDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, UpdateClientDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task ArchiveAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
}
