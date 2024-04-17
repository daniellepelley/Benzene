using Benzene.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Benzene.Microsoft.Logging;

public class MicrosoftBenzeneLogContext : IBenzeneLogContext
{
    private readonly ILogger _logger;

    public MicrosoftBenzeneLogContext(ILogger logger)
    {
        _logger = logger;
    }
    public IDisposable Create(IDictionary<string, string> properties)
    {
        return _logger.BeginScope(properties);
    }
}
