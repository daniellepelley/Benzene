using System;
using System.Collections.Generic;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

public class NullBenzeneLogContext : IBenzeneLogContext
{
    public IDisposable Create(IDictionary<string, string> properties)
    {
        return new NullDisposable();
    }
}