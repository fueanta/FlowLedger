using FluentValidation;

namespace FlowLedger.Application.BillingRequests;

public sealed class AddCommentDtoValidator : AbstractValidator<AddCommentDto>
{
    public AddCommentDtoValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
