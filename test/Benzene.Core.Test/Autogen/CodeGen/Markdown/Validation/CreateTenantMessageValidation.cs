using FluentValidation;
using Benzene.Test.Autogen.CodeGen.Model;

namespace Benzene.Test.Autogen.CodeGen.Markdown.Validation;

public class CreateTenantMessageValidation : AbstractValidator<CreateTenantMessage>
{
    public CreateTenantMessageValidation()
    {
        RuleFor(x => x.Name).NotNull().MaximumLength(10);
        RuleFor(x => x.Crn).NotEmpty().MaximumLength(30);
    }
}

public class CreateUserMessageValidation : AbstractValidator<CreateUserMessage>
{
    public CreateUserMessageValidation()
    {
        RuleFor(x => x.Name).NotNull().MaximumLength(10);
        RuleFor(x => x.Tenants).NotEmpty();
    }
}
    
public class GetUserMessageValidation : AbstractValidator<GetUserMessage>
{
    public GetUserMessageValidation()
    {
        RuleFor(x => x.Id).NotNull().MaximumLength(10);
    }
}
public class GetTenantMessageValidation : AbstractValidator<GetTenantMessage>
{
    public GetTenantMessageValidation()
    {
        RuleFor(x => x.Id).NotNull();
    }
}
