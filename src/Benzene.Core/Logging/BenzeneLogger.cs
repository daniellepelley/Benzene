using System;
using System.Collections.Generic;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

public class BenzeneLogger : IBenzeneLogger
{
    private readonly IEnumerable<IBenzeneLogAppender> _benzeneLogAppenders;

    public BenzeneLogger(IEnumerable<IBenzeneLogAppender> benzeneLogAppenders)
    {
        _benzeneLogAppenders = benzeneLogAppenders;
    }

    public void Log(BenzeneLogLevel benzeneLogLevel, Exception? exception, string message, params object[] args)
    {
        foreach (var benzeneLogAppender in _benzeneLogAppenders)
        {
            benzeneLogAppender.Log(benzeneLogLevel, exception, message, args);
        }
    }

    public static IBenzeneLogger NullLogger => new NullBenzeneLogger();
}