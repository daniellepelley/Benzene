using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsBooleanValidator<T>: PropertyValidator<T, string>, INotEmptyValidator
    {
        public override string Name => "IsBooleanValidator";

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            return string.IsNullOrEmpty(value) || bool.TryParse(value, out _);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "{PropertyName} must be a valid Boolean";
        }
    }
}