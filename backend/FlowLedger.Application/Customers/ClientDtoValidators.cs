using FluentValidation;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Customers;

public sealed class CreateClientDtoValidator : AbstractValidator<CreateClientDto>
{
    public CreateClientDtoValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TaxIdentifier).MaximumLength(80);
    }
}

public sealed class UpdateClientDtoValidator : AbstractValidator<UpdateClientDto>
{
    public UpdateClientDtoValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TaxIdentifier).MaximumLength(80);
        RuleFor(x => x.Status).Must(x => x is ClientStatus.Active or ClientStatus.Inactive);
    }
}
