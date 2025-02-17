using System.Text;
using Xunit;
using zipkin4net.Tracers.Zipkin;
using zipkin4net;
using zipkin4net.Transport.Http;
using ILogger = zipkin4net.ILogger;

namespace Benzene.Integration.Test;
public class ZipkinLogger : ILogger
{
    public void LogInformation(string message)
    {
        
    }

    public void LogWarning(string message)
    {
        
    }

    public void LogError(string message)
    {
        
    }
}