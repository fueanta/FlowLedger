using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;
using FlowLedger.Application.Customers;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Common;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Customers;

public sealed class CustomerService : ICustomerService
{
    private const int MaxExportRows = 5000;

    private readonly FlowLedgerDbContext _dbContext;
    private readonly ICsvExportService _csvExportService;

    public CustomerService(FlowLedgerDbContext dbContext, ICsvExportService csvExportService)
    {
        _dbContext = dbContext;
        _csvExportService = csvExportService;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.Status == ClientStatus.Active)
            .OrderBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<CustomerDto>> GetAsync(ClientQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureCanView(currentUser);

        var page = PagingQueryGuard.Page(query.Page);
        var pageSize = PagingQueryGuard.PageSize(query.PageSize);
        var clients = ApplySort(BuildListQuery(query), query.SortBy, query.SortDirection);

        var totalCount = await clients.CountAsync(cancellationToken);
        var items = await clients
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items, page, pageSize, totalCount);
    }

    public async Task<CsvResult> ExportCsvAsync(ClientQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureCanView(currentUser);

        var rows = await ApplySort(BuildListQuery(query), query.SortBy, query.SortDirection)
            .Take(MaxExportRows)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return _csvExportService.Export(
            $"clients-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            rows,
            [
                new("Company Name", x => x.Name),
                new("Contact Person", x => x.ContactPerson),
                new("Email", x => x.ContactEmail),
                new("Phone", x => x.Phone),
                new("Status", x => x.Status),
                new("Tax Identifier", x => x.TaxIdentifier),
                new("Created At UTC", x => x.CreatedAtUtc),
                new("Updated At UTC", x => x.UpdatedAtUtc),
                new("Archived At UTC", x => x.ArchivedAtUtc)
            ]);
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureCanView(currentUser);

        var client = await _dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return client is null
            ? throw new KeyNotFoundException("Client was not found.")
            : ToDto(client);
    }

    public async Task<Guid> CreateAsync(CreateClientDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureCanCreate(currentUser);
        await EnsureEmailAvailableAsync(request.Email, null, cancellationToken);

        var now = DateTime.UtcNow;
        var client = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName.Trim(),
            ContactPerson = request.ContactPerson.Trim(),
            ContactEmail = request.Email.Trim(),
            Phone = request.Phone?.Trim() ?? string.Empty,
            BillingAddress = request.Address.Trim(),
            TaxIdentifier = request.TaxIdentifier?.Trim() ?? string.Empty,
            Status = ClientStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Customers.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return client.Id;
    }

    public async Task UpdateAsync(Guid id, UpdateClientDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureCanEdit(currentUser);

        var client = await _dbContext.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (client is null)
        {
            throw new KeyNotFoundException("Client was not found.");
        }

        if (client.Status == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot be edited.");
        }

        await EnsureEmailAvailableAsync(request.Email, id, cancellationToken);

        client.Name = request.CompanyName.Trim();
        client.ContactPerson = request.ContactPerson.Trim();
        client.ContactEmail = request.Email.Trim();
        client.Phone = request.Phone?.Trim() ?? string.Empty;
        client.BillingAddress = request.Address.Trim();
        client.TaxIdentifier = request.TaxIdentifier?.Trim() ?? string.Empty;
        client.Status = request.Status;
        client.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureCanArchive(currentUser);

        var client = await _dbContext.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (client is null)
        {
            throw new KeyNotFoundException("Client was not found.");
        }

        if (client.Status == ClientStatus.Archived)
        {
            return;
        }

        var now = DateTime.UtcNow;
        client.Status = ClientStatus.Archived;
        client.ArchivedAtUtc = now;
        client.ArchivedByUserId = currentUser.Id;
        client.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEmailAvailableAsync(string email, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalized = email.Trim();
        var exists = await _dbContext.Customers.AnyAsync(
            x => x.ContactEmail == normalized && (currentId == null || x.Id != currentId),
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Client email is already in use.");
        }
    }

    private static IQueryable<Customer> ApplySort(IQueryable<Customer> query, string? sortBy, string? sortDirection)
    {
        var descending = PagingQueryGuard.Descending(sortDirection);
        var key = PagingQueryGuard.SortBy(sortBy, "companyName", "companyName", "status", "createdAtUtc", "updatedAtUtc").ToLowerInvariant();

        return key switch
        {
            "status" => descending ? query.OrderByDescending(x => x.Status).ThenBy(x => x.Name) : query.OrderBy(x => x.Status).ThenBy(x => x.Name),
            "createdatutc" => descending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc),
            "updatedatutc" => descending ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.UpdatedAtUtc),
            _ => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)
        };
    }

    private IQueryable<Customer> BuildListQuery(ClientQuery query)
    {
        var clients = _dbContext.Customers.AsNoTracking().AsQueryable();

        if (query.Status is not null)
        {
            clients = clients.Where(x => x.Status == query.Status);
        }

        var search = PagingQueryGuard.Search(query.Search);
        if (search is not null)
        {
            clients = clients.Where(x =>
                x.Name.Contains(search) ||
                x.ContactEmail.Contains(search) ||
                x.ContactPerson.Contains(search));
        }

        return clients;
    }

    private static CustomerDto ToDto(Customer client)
    {
        return new CustomerDto(
            client.Id,
            client.Name,
            client.ContactPerson,
            client.ContactEmail,
            client.Phone,
            client.BillingAddress,
            client.TaxIdentifier,
            client.Status,
            client.CreatedAtUtc,
            client.UpdatedAtUtc,
            client.ArchivedAtUtc);
    }

    private static void EnsureCanView(CurrentUser currentUser)
    {
        if (currentUser.Role is not (RoleName.Sales or RoleName.Accounts or RoleName.Manager or RoleName.Admin))
        {
            throw new UnauthorizedAccessException("Only internal users can view clients.");
        }
    }

    private static void EnsureCanCreate(CurrentUser currentUser)
    {
        if (currentUser.Role is not (RoleName.Sales or RoleName.Accounts or RoleName.Admin))
        {
            throw new UnauthorizedAccessException("Only Sales, Accounts, or Admin users can create clients.");
        }
    }

    private static void EnsureCanEdit(CurrentUser currentUser)
    {
        if (currentUser.Role is not (RoleName.Accounts or RoleName.Admin))
        {
            throw new UnauthorizedAccessException("Only Accounts or Admin users can edit clients.");
        }
    }

    private static void EnsureCanArchive(CurrentUser currentUser)
    {
        if (!currentUser.IsAdmin)
        {
            throw new UnauthorizedAccessException("Only Admin users can archive clients.");
        }
    }
}
