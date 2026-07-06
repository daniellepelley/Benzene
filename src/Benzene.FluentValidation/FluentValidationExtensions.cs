using FluentValidation;
using Benzene.Results;

namespace Benzene.FluentValidation;

public static class FluentValidationExtensions
{
    public const string BenzeneStatusKey = "BenzeneStatus";

    public static IRuleBuilderOptions<T, TProperty> WithStatus<T, TProperty>(this IRuleBuilderOptions<T, TProperty> ruleBuilder, string status)
    {
        return ruleBuilder.WithState(_ => new BenzeneValidationState { Status = status });
    }
}

public class BenzeneValidationState
{
    public string Status { get; set; } = BenzeneResultStatus.ValidationError;
}
