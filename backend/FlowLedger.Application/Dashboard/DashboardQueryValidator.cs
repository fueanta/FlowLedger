using FluentValidation;

namespace FlowLedger.Application.Dashboard;

public sealed class DashboardQueryValidator : AbstractValidator<DashboardQuery>
{
    private static readonly int[] AllowedPeriods = [1, 3, 6, 12];

    public DashboardQueryValidator()
    {
        RuleFor(x => x.PeriodMonths)
            .Must(x => AllowedPeriods.Contains(x))
            .WithMessage("PeriodMonths must be one of 1, 3, 6, or 12.");
    }
}
