using FlowLedger.Application.Common;
using FluentValidation;

namespace FlowLedger.Application.Configuration;

public sealed record SystemSettingsDto(
    decimal VatPercentage,
    decimal ManagerApprovalThreshold,
    int InvoiceDueDays);

public sealed record UpdateSystemSettingsDto(
    decimal VatPercentage,
    decimal ManagerApprovalThreshold,
    int InvoiceDueDays);

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task UpdateAsync(UpdateSystemSettingsDto request, CurrentUser currentUser, CancellationToken cancellationToken);
}

public sealed class UpdateSystemSettingsDtoValidator : AbstractValidator<UpdateSystemSettingsDto>
{
    public UpdateSystemSettingsDtoValidator()
    {
        RuleFor(x => x.VatPercentage)
            .InclusiveBetween(0m, 30m);

        RuleFor(x => x.ManagerApprovalThreshold)
            .GreaterThan(0m);

        RuleFor(x => x.InvoiceDueDays)
            .InclusiveBetween(1, 365);
    }
}
