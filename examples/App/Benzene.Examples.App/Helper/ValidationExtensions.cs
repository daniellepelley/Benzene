using FluentValidation;

namespace Benzene.Examples.App.Helper;

public static class ValidationExtensions
{
	public static IRuleBuilderOptions<T, string> IsGuid<T>(this IRuleBuilder<T, string> ruleBuilder)
		=> ruleBuilder.SetValidator(new IsGuidValidator<T>());
}