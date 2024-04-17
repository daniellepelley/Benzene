using System;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsGuidValidator<T> : PropertyValidator<T, string>, INotEmptyValidator
    {
        public override string Name => "IsGuidValidator";

        public override bool IsValid(ValidationContext<T> context, string value)
        {

            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            return Guid.TryParse(value, out _);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "{PropertyName} must be a valid Guid";
        }
    }
}