using FluentValidation;

namespace FlowLedger.Application.BillingRequests;

public sealed class ApproveBillingRequestDtoValidator : AbstractValidator<ApproveBillingRequestDto>
{
    public ApproveBillingRequestDtoValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(2000);
    }
}
