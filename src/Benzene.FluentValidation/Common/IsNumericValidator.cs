using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsNumericValidator<T> : PropertyValidator<T, string>, INotEmptyValidator
    {
        public override string Name => "IsNumericValidator";

        public override bool IsValid(ValidationContext<T> context, string value)
        {

            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            return value.All(char.IsDigit);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "{PropertyName} must be numeric";
        }
    }
}