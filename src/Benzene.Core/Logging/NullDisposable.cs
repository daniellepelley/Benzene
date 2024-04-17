using System;

namespace Benzene.Core.Logging;

public class NullDisposable : IDisposable
{
    public void Dispose()
    {
        // Null Disposable
    }
}