using System;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

internal class NullBenzeneLogger : IBenzeneLogger
{
    public void Log(BenzeneLogLevel benzeneLogLevel, Exception exception, string message, params object[] args)
    {
    }
}