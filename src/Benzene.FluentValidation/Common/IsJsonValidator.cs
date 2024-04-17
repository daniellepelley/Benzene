using System.Text.Json;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsJsonValidator<T> : PropertyValidator<T, string>, INotEmptyValidator
    {
        public override string Name => "IsJsonValidator";

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            try
            {
                JsonDocument.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "{PropertyName} must be valid JSON";
        }
    }
}
