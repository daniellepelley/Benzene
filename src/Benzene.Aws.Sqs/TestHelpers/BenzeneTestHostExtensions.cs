using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions;

namespace Benzene.Aws.Sqs.TestHelpers;

public static class BenzeneTestHostExtensions
{
    public static Task<SQSBatchResponse> SendSqsAsync(this IBenzeneTestHost source, SQSEvent sqsEvent)
    {
        return source.SendEventAsync<SQSBatchResponse>(sqsEvent);
    }

    public static Task<SQSBatchResponse> SendSqsAsync(this IBenzeneTestHost source, IMessageBuilder messageBuilder)
    {
        return source.SendSqsAsync(messageBuilder.AsSqs());
    }
}