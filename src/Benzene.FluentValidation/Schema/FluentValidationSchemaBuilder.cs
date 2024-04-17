using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentValidation;
using FluentValidation.Validators;
using Benzene.Abstractions.Validation;
using Benzene.Core.Helper;
using Benzene.Core.Validation;
using Benzene.FluentValidation.Common;

namespace Benzene.FluentValidation.Schema;

public class FluentValidationSchemaBuilder : IValidationSchemaBuilder
{
    private readonly IValidator[] _validators;

    public FluentValidationSchemaBuilder(params Assembly[] assemblies)
        :this(Utils.GetAllTypes(assemblies).ToArray())
    { }

    public FluentValidationSchemaBuilder(params Type[] types)
    {
        _validators = types
            .Where(t => typeof(IValidator).IsAssignableFrom(t))
            .Select(x => Activator.CreateInstance(x) as IValidator)
            .ToArray();
    }

    public FluentValidationSchemaBuilder(params IValidator[] validators)
    {
        _validators = validators;
    }

    public IDictionary<string, IValidationSchema[]> GetValidationSchemas(Type type)
    {
        var validator = _validators.FirstOrDefault(validator =>
            validator.GetType().BaseType.GetGenericArguments()[0] == type);

        if (validator == null)
        {
            return new Dictionary<string, IValidationSchema[]>();
        }

        return GetRules(validator);
    }

    public static IDictionary<string, IValidationSchema[]> GetRules(IValidator validator)
    {
        var r = validator.CreateDescriptor().GetMembersWithValidators();

        var dictionary = new Dictionary<string, IValidationSchema[]>();

        foreach (var t in r)
        {
            var rules = t.Select(x => GetRule(x.Validator))
                .Where(x => x != null)
                .ToArray();

            if (!string.IsNullOrEmpty(t.Key))
            {
                dictionary.Add(t.Key, rules);
            }
        }

        return dictionary;
    }

    public static IValidationSchema GetRule(IPropertyValidator propertyValidator)
    {
        var name = propertyValidator.Name;

        switch (name)
        {
            case "MinimumLengthValidator":
                var minimumLengthValidator = (IMinimumLengthValidator)propertyValidator;
                return new MinLengthValidationSchema(minimumLengthValidator);
            case "MaximumLengthValidator":
                var maximumLengthValidator = (IMaximumLengthValidator)propertyValidator;
                return new MaxLengthValidationSchema(maximumLengthValidator);
            case "IsGuidValidator":
                return new ValidationSchema(ValidationConstants.IsGuid, "Is Guid");
            case "NotEmptyValidator":
                return new ValidationSchema(ValidationConstants.NotEmpty, "Not Empty" );
            case "NotNullValidator":
                return new ValidationSchema(ValidationConstants.NotNull, "Not Null" );
            case "IsOneOfValidator":
                var isOneOfValidator = (IIsOneOfValidator)propertyValidator;
                return new IsOneOfValidationSchema(isOneOfValidator); 
            case "RegularExpressionValidator":
                var regularExpressionValidator = (IRegularExpressionValidator)propertyValidator;
                return new RegexValidationSchema(regularExpressionValidator);
            case "EmailValidator":
                return new ValidationSchema( ValidationConstants.Email, "Is Valid Email" );
            case "IsPhoneNumberValidator":
                return new ValidationSchema(ValidationConstants.Phone, "Is Valid Phone Number");
            case "IsNumericValidator":
                return new ValidationSchema(ValidationConstants.IsNumeric, "Is Numeric");
            case "IsDoubleGuidValidator":
                return new ValidationSchema(ValidationConstants.IsNumeric, "Is Numeric");
            case "IsLettersOrSymbolsValidator":
                return new ValidationSchema(ValidationConstants.IsLettersOrSymbols, "Is Letters Or Symbols");
             case "IsNumbersOrSymbolsValidator":
                return new ValidationSchema(ValidationConstants.IsNumbersOrSymbols, "Is Numbers Or Symbols");
             case "IsAlphaNumericAndSymbolsValidator":
                return new ValidationSchema(ValidationConstants.IsAlphaNumericOrSymbols, "Is AlphaNumeric or Symbols");
             case "IsBooleanValidator":
                return new ValidationSchema(ValidationConstants.IsBoolean, "Is Boolean");
            case "GreaterThanOrEqualValidator":
                var greaterThanOrEqualValidator = (IGreaterThanOrEqualValidator)propertyValidator;
                return new ValidationSchema(ValidationConstants.GreaterThanOrEqual, $"Greater Than Or Equal To {greaterThanOrEqualValidator.ValueToCompare}");
            case "LessThanOrEqualValidator":
                var lessThanOrEqualValidator = (ILessThanOrEqualValidator)propertyValidator;
                return new ValidationSchema(ValidationConstants.LessThanOrEqual, $"Less Than Or Equal To {lessThanOrEqualValidator.ValueToCompare}");
            case "GreaterThanValidator":
                var greaterThanValidator = (IComparisonValidator)propertyValidator;
                return new ValidationSchema(ValidationConstants.GreaterThan, $"Greater Than {greaterThanValidator.ValueToCompare}");
            case "LessThanValidator":
                var lessThanValidator = (IComparisonValidator)propertyValidator;
                return new ValidationSchema(ValidationConstants.LessThan, $"Less Than {lessThanValidator.ValueToCompare}");
            case "NotEqualValidator":
                var comparisonValidator= (IComparisonValidator)propertyValidator;
                return new ValidationSchema(ValidationConstants.NotEqual,$"Not Equal To {comparisonValidator.MemberToCompare.Name}");
            default:
                return null;
        }
    }


}
