using FlowLedger.Application.Auth;
using FlowLedger.Application.Common;
using FlowLedger.Domain.Enums;
using FluentValidation;

namespace FlowLedger.Application.Users;

public sealed record UserQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    RoleName? Role = null,
    UserStatus? Status = null,
    string? SortBy = "fullName",
    string? SortDirection = "asc");

public sealed record UpdateUserRoleDto(RoleName Role);

public interface IUserAdminService
{
    Task<PagedResult<UserDto>> GetAsync(UserQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<UserDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task UpdateRoleAsync(Guid id, UpdateUserRoleDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task ActivateAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
}

public sealed class UpdateUserRoleDtoValidator : AbstractValidator<UpdateUserRoleDto>
{
    public UpdateUserRoleDtoValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
