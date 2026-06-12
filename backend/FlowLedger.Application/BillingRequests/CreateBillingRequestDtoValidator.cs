using FluentValidation;

namespace FlowLedger.Application.BillingRequests;

public sealed class CreateBillingRequestDtoValidator : AbstractValidator<CreateBillingRequestDto>
{
    public CreateBillingRequestDtoValidator()
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

public sealed class CreateBillingRequestLineItemDtoValidator : AbstractValidator<CreateBillingRequestLineItemDto>
{
    public CreateBillingRequestLineItemDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0);
    }
}
