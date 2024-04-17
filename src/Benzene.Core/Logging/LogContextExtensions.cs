using System;
using System.Collections.Generic;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

public static class LogContextExtensions
{
    public static IDisposable Create(this IBenzeneLogContext source, string key, string value)
    {
        return source.Create(new Dictionary<string, string>
        {
            { key, value }
        });
    } 
}
