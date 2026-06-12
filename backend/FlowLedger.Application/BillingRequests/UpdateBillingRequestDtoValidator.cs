using FluentValidation;

namespace FlowLedger.Application.BillingRequests;

public sealed class UpdateBillingRequestDtoValidator : AbstractValidator<UpdateBillingRequestDto>
{
    public UpdateBillingRequestDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.LineItems)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.LineItems)
            .SetValidator(new CreateBillingRequestLineItemDtoValidator());
    }
}
