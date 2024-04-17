﻿using Benzene.Abstractions.Validation;

namespace Benzene.Core.Validation;

public class ValidationSchema : IValidationSchema
{
    public ValidationSchema(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; init; }
    public string Description { get; init; }
}
