using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string ContactPerson,
    string ContactEmail,
    string Phone,
    string BillingAddress,
    string TaxIdentifier,
    ClientStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ArchivedAtUtc);

public sealed record CreateClientDto(
    string CompanyName,
    string ContactPerson,
    string Email,
    string? Phone,
    string Address,
    string? TaxIdentifier);

public sealed record UpdateClientDto(
    string CompanyName,
    string ContactPerson,
    string Email,
    string? Phone,
    string Address,
    string? TaxIdentifier,
    ClientStatus Status);

public sealed record ClientQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    ClientStatus? Status = null,
    string? SortBy = "companyName",
    string? SortDirection = "asc");
