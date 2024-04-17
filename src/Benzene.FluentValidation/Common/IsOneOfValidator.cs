using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.FluentValidation.Common
{
    public class IsOneOfValidator<T> : PropertyValidator<T, string>, INotEmptyValidator, IIsOneOfValidator
    {
        public string[] Options { get; }
        public override string Name => "IsOneOfValidator";

        public IsOneOfValidator(string[] options)
        {
            Options = options;
        }

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            return string.IsNullOrEmpty(value) || Options.Contains(value);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            var optionsText = string.Join(", ", Options);
            return $"{{PropertyName}} must equal one of {optionsText}.";
        }
    }
}