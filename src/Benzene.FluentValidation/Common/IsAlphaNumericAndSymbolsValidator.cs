using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsAlphaNumericAndSymbolsValidator<T> : PropertyValidator<T, string>, INotEmptyValidator
    {
        private readonly char[] _validChars;
        public override string Name => "IsAlphaNumericAndSymbolsValidator";

        public IsAlphaNumericAndSymbolsValidator(params char[] validChars)
        { 
            _validChars = validChars;
        }

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            return string.IsNullOrEmpty(value) || value.All(IsValid);
        }

        private bool IsValid(char value)
        {
            return char.IsLetter(value) ||
                   char.IsNumber(value) || 
                   _validChars.Contains(value);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "{PropertyName} must be a number or letter only";
        }
    }
}