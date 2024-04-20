using System;
using System.Collections.Concurrent;
using Amazon.Lambda.Core;

namespace Benzene.Examples.Aws.Tests.Helpers;

public class ThreadSafeTestLambdaLogger : ILambdaLogger
{
    private readonly ConcurrentBag<string> _buffer = new ConcurrentBag<string>();

    public string[] Logs => _buffer.ToArray();

    public void Log(string message) => LogLine(message);

    public void LogLine(string message)
    {
        _buffer.Add(message);
        Console.WriteLine(message);
    }
}