using System;
using System.Collections.Generic;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

/// <summary>
/// Provides a null object pattern implementation of log context that performs no operations.
/// </summary>
/// <remarks>
/// This implementation is useful when logging context functionality is not needed or during testing.
/// It returns a null disposable that performs no cleanup when disposed.
/// </remarks>
public class NullBenzeneLogContext : IBenzeneLogContext
{
    /// <summary>
    /// Creates a no-op disposable scope that performs no logging context operations.
    /// </summary>
    /// <param name="properties">The properties to add to the log context (ignored).</param>
    /// <returns>A null disposable that performs no operations.</returns>
    public IDisposable Create(IDictionary<string, string> properties)
    {
        return new NullDisposable();
    }
}