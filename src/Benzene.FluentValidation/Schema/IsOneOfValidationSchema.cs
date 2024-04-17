using System.Linq;
using Benzene.Abstractions.Validation;
using Benzene.Core.Validation;
using Benzene.FluentValidation.Common;

namespace Benzene.FluentValidation.Schema;

public class IsOneOfValidationSchema : ValidationSchema, IIsOneOfValidationSchema
{
    public string[] Options { get; }
    
    public IsOneOfValidationSchema(IIsOneOfValidator isOneOfValidator)
        : base(ValidationConstants.IsOneOf, $"Is one of {string.Join(", ", isOneOfValidator.Options.Select(x => $"'{x}'"))}")
    {
        Options = isOneOfValidator.Options;
    }
}