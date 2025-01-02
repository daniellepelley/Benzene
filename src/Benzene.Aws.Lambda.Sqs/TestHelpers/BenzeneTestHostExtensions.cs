using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions;

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
}