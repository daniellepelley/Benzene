using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.TestHelpers;

namespace Benzene.Aws.Lambda.Sqs.TestHelpers;

public static class BenzeneTestHostExtensions
{
    public static Task<SQSBatchResponse> SendSqsAsync(this IBenzeneTestHost source, SQSEvent sqsEvent)
    {
        return source.SendEventAsync<SQSBatchResponse>(sqsEvent);
    }

    public static Task<SQSBatchResponse> SendSqsAsync<T>(this IBenzeneTestHost source, IMessageBuilder<T> messageBuilder)
    {
        return source.SendSqsAsync(messageBuilder.AsSqs());
    }

    // Overloads that dispatch straight off the IAwsLambdaEntryPoint that BuildAwsLambdaHost() returns,
    // so BuildAwsLambdaHost().SendSqsAsync(...) compiles as a one-liner without a manual
    // `new AwsLambdaBenzeneTestHost(...)` wrap. The caller owns the entry point's lifetime, so this
    // transient host wrapper does NOT dispose it.
    public static Task<SQSBatchResponse> SendSqsAsync(this IAwsLambdaEntryPoint source, SQSEvent sqsEvent)
    {
        return new AwsLambdaBenzeneTestHost(source).SendSqsAsync(sqsEvent);
    }

    public static Task<SQSBatchResponse> SendSqsAsync<T>(this IAwsLambdaEntryPoint source, IMessageBuilder<T> messageBuilder)
    {
        return new AwsLambdaBenzeneTestHost(source).SendSqsAsync(messageBuilder);
    }
}