using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Benzene.Test.Logging.Helpers;

public class FakeLogEntry
{
    public FakeLogEntry(string category, LogLevel level, string message, Exception exception, IReadOnlyList<object> scopes)
    {
        Category = category;
        Level = level;
        Message = message;
        Exception = exception;
        Scopes = scopes;
    }

    public string Category { get; }
    public LogLevel Level { get; }
    public string Message { get; }
    public Exception Exception { get; }
    public IReadOnlyList<object> Scopes { get; }
}

public class FakeLogCollector
{
    private readonly object _lock = new();
    private readonly List<FakeLogEntry> _entries = new();
    private readonly List<object> _scopeStates = new();
    private readonly AsyncLocal<Stack<object>> _activeScopes = new();

    public FakeLogEntry[] Entries
    {
        get { lock (_lock) { return _entries.ToArray(); } }
    }

    /// <summary>Every state object ever passed to BeginScope, in order.</summary>
    public object[] ScopeStates
    {
        get { lock (_lock) { return _scopeStates.ToArray(); } }
    }

    public IDictionary<string, object>[] ScopeDictionaries =>
        ScopeStates.OfType<IEnumerable<KeyValuePair<string, object>>>()
            .Select(x => (IDictionary<string, object>)x.ToDictionary(kv => kv.Key, kv => kv.Value))
            .ToArray();

    internal void Add(FakeLogEntry entry)
    {
        lock (_lock) { _entries.Add(entry); }
    }

    internal IDisposable PushScope(object state)
    {
        lock (_lock) { _scopeStates.Add(state); }
        var stack = _activeScopes.Value ??= new Stack<object>();
        stack.Push(state);
        return new ScopePopper(stack);
    }

    internal IReadOnlyList<object> CurrentScopes => _activeScopes.Value?.ToArray() ?? Array.Empty<object>();

    private class ScopePopper : IDisposable
    {
        private readonly Stack<object> _stack;
        private bool _disposed;

        public ScopePopper(Stack<object> stack) => _stack = stack;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_stack.Count > 0)
            {
                _stack.Pop();
            }
        }
    }
}

public class FakeLogger : ILogger
{
    private readonly FakeLogCollector _collector;
    private readonly string _category;

    public FakeLogger(FakeLogCollector collector, string category)
    {
        _collector = collector;
        _category = category;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _collector.Add(new FakeLogEntry(_category, logLevel, formatter(state, exception), exception, _collector.CurrentScopes));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => _collector.PushScope(state);
}

public class FakeLogger<T> : FakeLogger, ILogger<T>
{
    public FakeLogger(FakeLogCollector collector)
        : base(collector, typeof(T).Name)
    {
    }
}

public class FakeLoggerFactory : ILoggerFactory
{
    public FakeLogCollector Collector { get; } = new();

    public ILogger CreateLogger(string categoryName) => new FakeLogger(Collector, categoryName);

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
