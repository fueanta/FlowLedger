using FluentValidation;

namespace FlowLedger.Application.BillingRequests;

public sealed class RejectBillingRequestDtoValidator : AbstractValidator<RejectBillingRequestDto>
{
    public RejectBillingRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
