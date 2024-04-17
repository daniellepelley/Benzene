using System;
using System.Collections.Generic;
using Benzene.Abstractions.Validation;

namespace Benzene.Core.Validation;

public class BlankValidationSchemaBuilder : IValidationSchemaBuilder
{
    public IDictionary<string, IValidationSchema[]> GetValidationSchemas(Type type)
    {
        return new Dictionary<string, IValidationSchema[]>();
    }
}
