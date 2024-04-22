using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Middleware;

namespace Benzene.Aws.Sqs;

public class SqsApplication : IMiddlewareApplication<SQSEvent, SQSBatchResponse>
{
    private readonly IMiddlewarePipeline<SqsMessageContext> _pipeline;

    public SqsApplication(IMiddlewarePipeline<SqsMessageContext> pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task<SQSBatchResponse> HandleAsync(SQSEvent @event, IServiceResolver serviceResolver)
    {
        var batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();
        var tasks = @event.Records.Select(record => SqsMessageContext.CreateInstance(@event, record)).Select(async context =>
            {
                try
                {

                    using (var scope = serviceResolver.GetService<IServiceResolverFactory>().CreateScope())
                    {
                        var setCurrentTransport = scope.GetService<ISetCurrentTransport>();
                        setCurrentTransport.SetTransport("sqs");
                        await _pipeline.HandleAsync(context, scope);
                    }

                    if (!context.MessageResult.IsSuccessful)
                    {
                        batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = context.SqsMessage.MessageId});
                    }
                }
                catch
                {
                    batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = context.SqsMessage.MessageId});
                }
            })
            .ToArray();

        await Task.WhenAll(tasks);
        return new SQSBatchResponse(batchItemFailures);
    }
}
