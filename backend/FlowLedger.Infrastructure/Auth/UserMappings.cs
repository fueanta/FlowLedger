using FlowLedger.Application.Auth;
using FlowLedger.Domain.Entities;

namespace FlowLedger.Infrastructure.Auth;

internal static class UserMappings
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(user.Id, user.FullName, user.Email, user.Role);
    }
}
