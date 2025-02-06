using System;
using Benzene.Abstractions;

namespace Benzene.Core.Correlation;

public class CorrelationId : ICorrelationId
{
    private string _correlationId = Guid.NewGuid().ToString();

    public void Set(string correlationId)
    {
        if (!string.IsNullOrEmpty(correlationId))
        {
            _correlationId = correlationId;
        }
    }

    public string Get()
    {
        return _correlationId;
    }
}