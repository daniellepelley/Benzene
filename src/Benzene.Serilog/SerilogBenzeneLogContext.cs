using Benzene.Abstractions.Logging;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Benzene.Serilog
{
    public class SerilogBenzeneLogContext : IBenzeneLogContext
    {
        public IDisposable Create(IDictionary<string, string> properties)
        {
            return LogContext.Push(properties
                .Select(x => new PropertyEnricher(x.Key, x.Value) as ILogEventEnricher)
                .ToArray());
        }
    }
}
