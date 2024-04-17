using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsLettersOrSymbolsValidator<T> : PropertyValidator<T, string>, INotEmptyValidator
    {
        private readonly char[] _validChars;
        public override string Name => "IsLettersOrSymbolsValidator";

        public IsLettersOrSymbolsValidator(params char[] validChars)
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
            return char.IsLetter(value) ||
                   _validChars.Contains(value);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "{PropertyName} must be a name";
        }
    }
}