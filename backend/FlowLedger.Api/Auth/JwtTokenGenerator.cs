using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlowLedger.Application.Auth;
using FlowLedger.Application.Configuration;
using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FlowLedger.Api.Auth;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private const int FallbackAccessTokenMinutes = 30;

    private readonly JwtOptions _options;
    private readonly IAppSettingReader _appSettingReader;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IAppSettingReader appSettingReader)
    {
        _options = options.Value;
        _appSettingReader = appSettingReader;
    }

    public async Task<string> GenerateTokenAsync(User user, CancellationToken cancellationToken)
    {
        var configuredMinutes = await _appSettingReader.ReadValueAsync(
            FlowLedgerSeedData.JwtAccessTokenMinutesKey,
            cancellationToken);
        var accessTokenMinutes = int.TryParse(configuredMinutes, out var parsedMinutes)
            ? parsedMinutes
            : FallbackAccessTokenMinutes;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim("role", user.Role.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
