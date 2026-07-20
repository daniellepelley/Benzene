using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.EventHub;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.EventHubWorker;

/// <summary>
/// Covers <see cref="BenzeneEventHubConfig.RaiseOnFailureStatus"/> (#30.5): a handler that returns a
/// failure result (without throwing) escalates into the same not-checkpointed path as an unhandled
/// exception, so the partition doesn't advance past the failed event. By default the failure result
/// is diagnostics-only and the partition checkpoints past it.
/// </summary>
public class EventHubWorkerRaiseOnFailureTest
{
    private static BenzeneEventHubWorker CreateWorker(bool? handlerSuccess, BenzeneEventHubConfig config)
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<EventHubConsumerContext>>();
        mockPipeline.Setup(x => x.HandleAsync(It.IsAny<EventHubConsumerContext>(), It.IsAny<IServiceResolver>()))
            .Callback<EventHubConsumerContext, IServiceResolver>((context, _) =>
            {
                if (handlerSuccess.HasValue)
                {
                    context.MessageResult = new MessageResult(handlerSuccess.Value);
                }
            })
            .Returns(Task.CompletedTask);

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<BenzeneEventHubWorker>>()).Returns(NullLogger<BenzeneEventHubWorker>.Instance);
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);

        return new BenzeneEventHubWorker(mockResolverFactory.Object,
            new EventHubConsumerApplication(mockPipeline.Object), config, Mock.Of<IEventProcessorClientFactory>());
    }

    private static async Task<bool> InvokeOnProcessEventAsync(BenzeneEventHubWorker worker)
    {
        var checkpointed = false;
        var partition = EventHubsModelFactory.PartitionContext("0");
        var args = new ProcessEventArgs(
            partition,
            new EventData(new BinaryData("some-message")),
            _ => { checkpointed = true; return Task.CompletedTask; },
            CancellationToken.None);

        var method = typeof(BenzeneEventHubWorker).GetMethod("OnProcessEventAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(worker, new object[] { args })!;

        return checkpointed;
    }

    [Fact]
    public async Task FailureResult_WithRaiseOnFailureStatus_DoesNotCheckpoint()
    {
        var worker = CreateWorker(handlerSuccess: false,
            new BenzeneEventHubConfig { RaiseOnFailureStatus = true, CatchHandlerExceptions = true });

        var checkpointed = await InvokeOnProcessEventAsync(worker);

        Assert.False(checkpointed);
    }

    [Fact]
    public async Task FailureResult_WithoutRaiseOnFailureStatus_CheckpointsAsBefore()
    {
        var worker = CreateWorker(handlerSuccess: false,
            new BenzeneEventHubConfig { RaiseOnFailureStatus = false });

        var checkpointed = await InvokeOnProcessEventAsync(worker);

        Assert.True(checkpointed);
    }

    [Fact]
    public async Task SuccessResult_WithRaiseOnFailureStatus_Checkpoints()
    {
        var worker = CreateWorker(handlerSuccess: true,
            new BenzeneEventHubConfig { RaiseOnFailureStatus = true });

        var checkpointed = await InvokeOnProcessEventAsync(worker);

        Assert.True(checkpointed);
    }

    [Fact]
    public void BenzeneEventHubConfig_RaiseOnFailureStatus_DefaultsFalse()
    {
        Assert.False(new BenzeneEventHubConfig().RaiseOnFailureStatus);
    }
}
