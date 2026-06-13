using System.Globalization;
using FlowLedger.Application.Common;
using FlowLedger.Application.Configuration;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Configuration;

public sealed class SystemSettingsService : ISystemSettingsService
{
    public const decimal DefaultVatPercentage = 15m;
    public const decimal DefaultManagerApprovalThreshold = 100000m;
    public const int DefaultInvoiceDueDays = 30;

    private readonly FlowLedgerDbContext _dbContext;

    public SystemSettingsService(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.AppSettings
            .AsNoTracking()
            .Where(x =>
                x.Key == FlowLedgerSeedData.VatPercentageKey ||
                x.Key == FlowLedgerSeedData.ManagerApprovalThresholdKey ||
                x.Key == FlowLedgerSeedData.InvoiceDueDaysKey)
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);

        return new SystemSettingsDto(
            ReadDecimal(settings, FlowLedgerSeedData.VatPercentageKey, DefaultVatPercentage),
            ReadDecimal(settings, FlowLedgerSeedData.ManagerApprovalThresholdKey, DefaultManagerApprovalThreshold),
            ReadInt(settings, FlowLedgerSeedData.InvoiceDueDaysKey, DefaultInvoiceDueDays));
    }

    public async Task UpdateAsync(UpdateSystemSettingsDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        if (currentUser.Role != RoleName.Admin)
        {
            throw new UnauthorizedAccessException("Only Admin users can update system settings.");
        }

        await UpsertAsync(
            FlowLedgerSeedData.VatPercentageKey,
            request.VatPercentage.ToString(CultureInfo.InvariantCulture),
            "VAT percentage used for new billing request totals.",
            cancellationToken);
        await UpsertAsync(
            FlowLedgerSeedData.ManagerApprovalThresholdKey,
            request.ManagerApprovalThreshold.ToString(CultureInfo.InvariantCulture),
            "Total amount above which Accounts approval routes to Management.",
            cancellationToken);
        await UpsertAsync(
            FlowLedgerSeedData.InvoiceDueDaysKey,
            request.InvoiceDueDays.ToString(CultureInfo.InvariantCulture),
            "Number of days after issue date used for new invoice due dates.",
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(string key, string value, string description, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.AppSettings.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (setting is null)
        {
            _dbContext.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                Description = description
            });
            return;
        }

        setting.Value = value;
        setting.Description = description;
    }

    private static decimal ReadDecimal(IReadOnlyDictionary<string, string> settings, string key, decimal fallback)
    {
        return settings.TryGetValue(key, out var value) && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> settings, string key, int fallback)
    {
        return settings.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }
}
