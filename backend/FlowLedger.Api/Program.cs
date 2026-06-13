using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using FlowLedger.Api.Auth;
using FlowLedger.Api.Validation;
using FlowLedger.Application.Auth;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Infrastructure.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var jwtOptions = BuildJwtOptions(builder);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddSingleton(Options.Create(jwtOptions));
builder.Services.AddValidatorsFromAssemblyContaining<CreateBillingRequestDtoValidator>();
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationActionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT bearer token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SalesOnly", policy => policy.RequireRole("Sales", "Admin"));
    options.AddPolicy("AccountsOnly", policy => policy.RequireRole("Accounts", "Admin"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("InternalUser", policy => policy.RequireRole("Sales", "Accounts", "Manager", "Admin"));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FlowLedgerWeb", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddInfrastructure(connectionString);
    builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
}

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseCors("FlowLedgerWeb");
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

if (!string.IsNullOrWhiteSpace(connectionString))
{
    await app.Services.MigrateDatabaseAsync();
    await app.Services.BootstrapSeedUserPasswordsAsync();

    if (!app.Environment.IsProduction() && app.Configuration.GetValue("SeedData:RefreshDemoDates", true))
    {
        await app.Services.RefreshDemoSeedDatesAsync();
    }
}

app.Run();

static JwtOptions BuildJwtOptions(WebApplicationBuilder builder)
{
    var options = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

    var issuer = string.IsNullOrWhiteSpace(options.Issuer) ? "FlowLedger" : options.Issuer;
    var audience = string.IsNullOrWhiteSpace(options.Audience) ? "FlowLedgerWeb" : options.Audience;
    var key = options.Key;

    if (string.IsNullOrWhiteSpace(key))
    {
        if (builder.Environment.IsProduction())
        {
            throw new InvalidOperationException("Jwt:Key must be configured in production.");
        }

        key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    if (Encoding.UTF8.GetByteCount(key) < 32)
    {
        throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
    }

    return new JwtOptions
    {
        Issuer = issuer,
        Audience = audience,
        Key = key
    };
}

public partial class Program;
