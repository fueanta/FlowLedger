using FlowLedger.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddInfrastructure(connectionString);
}

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

if (!string.IsNullOrWhiteSpace(connectionString))
{
    await app.Services.MigrateDatabaseAsync();
}

app.Run();

public partial class Program;
