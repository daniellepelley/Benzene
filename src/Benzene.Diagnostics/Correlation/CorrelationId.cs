using Benzene.Abstractions;

namespace Benzene.Diagnostics.Correlation;

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