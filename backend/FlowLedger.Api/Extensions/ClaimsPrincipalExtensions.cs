using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowLedger.Application.Common;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal user)
    {
        var idValue = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = user.FindFirstValue(JwtRegisteredClaimNames.Email);
        var name = user.FindFirstValue(JwtRegisteredClaimNames.Name);
        var roleValue = user.FindFirstValue("role");

        if (!Guid.TryParse(idValue, out var id) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(name) ||
            !Enum.TryParse<RoleName>(roleValue, out var role))
        {
            throw new UnauthorizedAccessException("Current user claims are invalid.");
        }

        return new CurrentUser(id, email, name, role);
    }
}
