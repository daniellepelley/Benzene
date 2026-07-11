using System;

namespace Benzene.Core.Logging;

/// <summary>
/// Provides a null object pattern implementation of IDisposable that performs no operations.
/// </summary>
/// <remarks>
/// This implementation is useful when a disposable is required but no cleanup action is needed.
/// Calling Dispose on this object has no effect.
/// </remarks>
public class NullDisposable : IDisposable
{
    /// <summary>
    /// Performs no operation. This is a null object pattern implementation.
    /// </summary>
    public void Dispose()
    {
        // Null Disposable
    }
}