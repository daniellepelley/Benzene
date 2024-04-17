using System;
using FluentValidation;

namespace Benzene.FluentValidation.Common
{
    public static class ExtensionMethods
    {
        public static IRuleBuilderOptions<T, string> IsGuid<T, TCompareTo>(this IRuleBuilder<T, string> ruleBuilder, Func<T, TCompareTo> propertyToCompareTo)
        {
            return ruleBuilder.SetValidator(new IsGuidValidator<T>());
        }

        public static IRuleBuilderOptions<T, string> IsGuid<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new IsGuidValidator<T>());
        }

        public static IRuleBuilderOptions<T, string> IsNumeric<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new IsNumericValidator<T>());
        }

        public static IRuleBuilderOptions<T, string> IsDoubleGuid<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new IsDoubleGuidValidator<T>());
        }

        public static IRuleBuilderOptions<T, string> IsJson<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new IsJsonValidator<T>());
        }
        public static IRuleBuilderOptions<T, string> IsOneOf<T>(this IRuleBuilder<T, string> ruleBuilder, params string[] options)
        {
            return ruleBuilder.SetValidator(new IsOneOfValidator<T>(options));
        }

        public static IRuleBuilderOptions<T, string> IsStrictAlphabetic<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Matches("^[a-zA-Z]*$");
        }

        public static IRuleBuilderOptions<T, string> IsLettersAndSymbols<T>(this IRuleBuilder<T, string> ruleBuilder, params char[] validChars)
        {
            return ruleBuilder.SetValidator(new IsLettersOrSymbolsValidator<T>(validChars));
        }

        public static IRuleBuilderOptions<T, string> IsAlphaNumericAndSymbols<T>(this IRuleBuilder<T, string> ruleBuilder, params char[] validChars)
        {
            return ruleBuilder.SetValidator(new IsAlphaNumericAndSymbolsValidator<T>(validChars));
        }

        public static IRuleBuilderOptions<T, string> IsNumbersAndSymbols<T>(this IRuleBuilder<T, string> ruleBuilder, params char[] validChars)
        {
            return ruleBuilder.SetValidator(new IsNumbersOrSymbolsValidator<T>(validChars));
        }

        public static IRuleBuilderOptions<T, string> IsBoolean<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new IsBooleanValidator<T>());
        }
    }
}