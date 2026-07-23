using System;
using System.Threading.Tasks;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.QueueStorage;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Middleware;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Messages.TestHelpers;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class QueueStoragePipelineTest
{
    private static string CreateEnvelopeMessageText()
    {
        var serializer = new JsonSerializer();
        return serializer.Serialize(MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsBenzeneMessage(serializer));
    }

    [Fact]
    public async Task Send_BenzeneMessageEnvelope_RoutesByEnvelopeTopic()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseQueueStorage(queue => queue
                    .UseBenzeneMessage(direct => direct
                        .UseMessageHandlers())))
            .Build();

        await app.HandleQueueMessage(CreateEnvelopeMessageText());

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public async Task Send_RawPayloadWithPresetTopic_RoutesByThePresetTopic()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseQueueStorage(queue => queue
                    .UsePresetTopic(Defaults.Topic)
                    .UseMessageHandlers()))
            .Build();

        // Raw payload, no envelope - the queue itself decides the topic.
        await app.HandleQueueMessage(Defaults.Message);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public async Task NonEnvelopeMessage_OnBenzeneMessagePipeline_IsDeferredWithoutError()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseQueueStorage(queue => queue
                    .UseBenzeneMessage(direct => direct
                        .UseMessageHandlers())))
            .Build();

        await app.HandleQueueMessage("just some text, not an envelope");

        mockExampleService.Verify(x => x.Register(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PipelineException_Propagates_SoTheHostRetriesAndPoisons()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseQueueStorage(queue => queue
                    .Use("Throw", (QueueStorageContext _, Func<Task> _) =>
                        throw new InvalidOperationException("handler failed"))))
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.HandleQueueMessage("boom"));
    }

    [Fact]
    public async Task MessageMetadata_IsAvailableOnTheContext()
    {
        QueueStorageContext observed = null;
        var insertedOn = DateTimeOffset.UtcNow;

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseQueueStorage(queue => queue
                    .Use("Capture", async (QueueStorageContext context, Func<Task> next) =>
                    {
                        observed = context;
                        await next();
                    })))
            .Build();

        await app.HandleQueueMessages(new QueueStorageMessage("some-text")
        {
            MessageId = "some-id",
            DequeueCount = 3,
            InsertedOn = insertedOn
        });

        Assert.NotNull(observed);
        Assert.Equal("some-text", observed.Message.MessageText);
        Assert.Equal("some-id", observed.Message.MessageId);
        Assert.Equal(3, observed.Message.DequeueCount);
        Assert.Equal(insertedOn, observed.Message.InsertedOn);
    }

    [Fact]
    public async Task EnvelopeHandlerReturnsFailure_RaiseOnFailureDefault_EscalatesToProcessingException()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseQueueStorage(queue => queue
                    .UseBenzeneMessage(direct => direct
                        .Use("Fail", (BenzeneMessageContext context, Func<Task> _) =>
                        {
                            context.MessageResult = BenzeneResult.ServiceUnavailable();
                            return Task.CompletedTask;
                        }))))
            .Build();

        // A failure recorded inside the (response-suppressed) envelope pipeline is now surfaced to the
        // outer QueueStorageContext, so RaiseOnFailureStatus (default true) escalates it into a thrown
        // exception - the host retries and eventually poisons the message instead of silently deleting it.
        await Assert.ThrowsAsync<QueueStorageMessageProcessingException>(
            () => app.HandleQueueMessage(CreateEnvelopeMessageText()));
    }

    [Fact]
    public async Task EnvelopeHandlerReturnsFailure_RaiseOnFailureDisabled_DoesNotEscalate()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseQueueStorage(queue => queue
                    .UseBenzeneMessage(direct => direct
                        .Use("Fail", (BenzeneMessageContext context, Func<Task> _) =>
                        {
                            context.MessageResult = BenzeneResult.ServiceUnavailable();
                            return Task.CompletedTask;
                        })),
                    configure: options => options.RaiseOnFailureStatus = false))
            .Build();

        // Opt-out (at-most-once): a returned failure is accepted like a success, so nothing is thrown.
        await app.HandleQueueMessage(CreateEnvelopeMessageText());
    }
}
