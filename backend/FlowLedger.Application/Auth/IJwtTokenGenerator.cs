using FlowLedger.Domain.Entities;

namespace FlowLedger.Application.Auth;

public interface IJwtTokenGenerator
{
    Task<string> GenerateTokenAsync(User user, CancellationToken cancellationToken);
}
