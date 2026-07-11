using System;
using System.Collections.Generic;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

/// <summary>
/// Provides extension methods for working with log contexts.
/// </summary>
public static class LogContextExtensions
{
    /// <summary>
    /// Creates a disposable log context scope with a single key-value pair.
    /// </summary>
    /// <param name="source">The log context to create the scope in.</param>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    /// <returns>A disposable scope that removes the log context when disposed.</returns>
    public static IDisposable Create(this IBenzeneLogContext source, string key, string value)
    {
        return source.Create(new Dictionary<string, string>
        {
            { key, value }
        });
    }
}
