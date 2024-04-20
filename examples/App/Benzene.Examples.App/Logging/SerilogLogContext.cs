using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Logging;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Benzene.Examples.App.Logging
{
    public class SerilogLogContext : ILogContext
    {
        public IDisposable Create(IDictionary<string, string> properties)
        {
            return LogContext.Push(properties
                .Select(x => new PropertyEnricher(x.Key, x.Value) as ILogEventEnricher)
                .ToArray());
        }
    }
}