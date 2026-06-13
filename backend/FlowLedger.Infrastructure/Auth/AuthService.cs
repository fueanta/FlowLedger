using FlowLedger.Application.Auth;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly FlowLedgerDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        FlowLedgerDbContext dbContext,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = user.LastLoginAtUtc.Value;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto(await _jwtTokenGenerator.GenerateTokenAsync(user, cancellationToken), user.ToDto());
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == userId && x.IsActive && x.Status == UserStatus.Active, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("Current user was not found.");
        }

        return user.ToDto();
    }
}
