using System;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsDoubleGuidValidator<T> : PropertyValidator<T, string>, INotEmptyValidator
    {
        public override string Name => "IsDoubleGuidValidator";

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            var parts = value.Split('|');

            return parts.Length == 2 &&
                Guid.TryParse(parts[0], out _) &&
                Guid.TryParse(parts[1], out _);

        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "{PropertyName} must be in the format 'Guid|Guid'";
        }
    }
}