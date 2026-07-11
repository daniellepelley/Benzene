using System;
using System.Collections.Generic;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

/// <summary>
/// Provides a composite logger that delegates logging calls to multiple log appenders.
/// </summary>
public class BenzeneLogger : IBenzeneLogger
{
    private readonly IEnumerable<IBenzeneLogAppender> _benzeneLogAppenders;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneLogger"/> class.
    /// </summary>
    /// <param name="benzeneLogAppenders">The collection of log appenders to delegate to.</param>
    public BenzeneLogger(IEnumerable<IBenzeneLogAppender> benzeneLogAppenders)
    {
        _benzeneLogAppenders = benzeneLogAppenders;
    }

    /// <summary>
    /// Logs a message at the specified log level to all registered appenders.
    /// </summary>
    /// <param name="benzeneLogLevel">The severity level of the log message.</param>
    /// <param name="exception">The exception to log, if any.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Optional arguments to format into the message template.</param>
    public void Log(BenzeneLogLevel benzeneLogLevel, Exception? exception, string message, params object[] args)
    {
        foreach (var benzeneLogAppender in _benzeneLogAppenders)
        {
            benzeneLogAppender.Log(benzeneLogLevel, exception, message, args);
        }
    }

    /// <summary>
    /// Gets a null object pattern logger that performs no operations.
    /// </summary>
    public static IBenzeneLogger NullLogger => new NullBenzeneLogger();
}