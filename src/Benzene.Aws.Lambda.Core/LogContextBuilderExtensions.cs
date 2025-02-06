using System;
using System.Linq;
using Benzene.Abstractions.Logging;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.Core;

public static class LogContextBuilderExtensions
{
    public static ILogContextBuilder<AwsEventStreamContext> WithRequestId(this ILogContextBuilder<AwsEventStreamContext> source)
    {
        return source.OnRequest("requestId", (_, context) => context.LambdaContext.AwsRequestId);
    }
    
    public static ILogContextBuilder<AwsEventStreamContext> WithApplication(this ILogContextBuilder<AwsEventStreamContext> source)
    {
        return source.OnRequest("application", (_, context) => GetApplicationName(context.LambdaContext.FunctionName));
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
