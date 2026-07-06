using System;
using System.Reflection;
using Benzene.Abstractions.Validation;
using Benzene.Results;
using FluentValidation.Results;

namespace Benzene.FluentValidation;

public class DefaultValidationStatusMapper : IValidationStatusMapper
{
    public string GetStatus(Type? handlerType, Type requestType, object? result)
    {
        if (result is ValidationResult validationResult)
        {
            foreach (var error in validationResult.Errors)
            {
                if (error.CustomState is BenzeneValidationState state)
                {
                    return state.Status;
                }
            }
        }

        if (handlerType != null)
        {
            var attribute = handlerType.GetCustomAttribute<ValidationStatusAttribute>();
            if (attribute != null)
            {
                return attribute.Status;
            }
        }

        return BenzeneResultStatus.ValidationError;
    }
}
