using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsNumbersOrSymbolsValidator<T> : PropertyValidator<T, string>, INotEmptyValidator
    {
        private readonly char[] _validChars;
        public override string Name => "IsNumbersOrSymbolsValidator";

        public IsNumbersOrSymbolsValidator(params char[] validChars)
        {
            _validChars = validChars;
        }

        public override bool IsValid(ValidationContext<T> context, string value)
        {

            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            return value.All(IsValid);
        }

        private bool IsValid(char value)
        {
            return char.IsNumber(value) ||
                   _validChars.Contains(value);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "{PropertyName} must be numeric";
        }
    }
}