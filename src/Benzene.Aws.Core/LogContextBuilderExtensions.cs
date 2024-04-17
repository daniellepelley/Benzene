using System;
using System.Linq;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.Logging;

namespace Benzene.Aws.Core;

public static class LogContextBuilderExtensions
{
    public static LogContextBuilder<AwsEventStreamContext> WithRequestId(this LogContextBuilder<AwsEventStreamContext> source)
    {
        return source.OnRequest("requestId", (resolver, context) => context.LambdaContext.AwsRequestId);
    }
    
    public static LogContextBuilder<AwsEventStreamContext> WithApplication(this LogContextBuilder<AwsEventStreamContext> source)
    {
        return source.OnRequest("application", (resolver, context) => GetApplicationName(context.LambdaContext.FunctionName));
    }
    
    private static string GetApplicationName(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input.Split("::", StringSplitOptions.RemoveEmptyEntries).First();
    }

}
