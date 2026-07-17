using System;
using Benzene.Abstractions.Validation;
using Benzene.Core.MessageHandlers;
using FluentValidation.Results;

namespace Benzene.FluentValidation;

public class DefaultValidationStatusMapper : IValidationStatusMapper
{
    private readonly IDefaultStatuses _defaultStatuses;

    public DefaultValidationStatusMapper(IDefaultStatuses defaultStatuses)
    {
        _defaultStatuses = defaultStatuses;
    }

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

        return _defaultStatuses.ValidationError;
    }
}
