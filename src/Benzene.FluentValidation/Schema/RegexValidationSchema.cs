using FluentValidation.Validators;
using Benzene.Abstractions.Validation;
using Benzene.Core.Validation;

namespace Benzene.FluentValidation.Schema;

public class RegexValidationSchema : ValidationSchema, IRegexValidationSchema
{
    public string Expression { get; }
    
    public RegexValidationSchema(IRegularExpressionValidator regularExpressionValidator)
        : base(ValidationConstants.Regex, $"Regex {regularExpressionValidator.Expression}")
    {
        Expression = regularExpressionValidator.Expression;
    }
}