using Amazon.SQS.Model;
using Benzene.Abstractions.DI;
using Benzene.Aws.Sqs.Consumer;

namespace Benzene.Aws.Sqs.TestHelpers;

/// <summary>
/// A test host that drives the standalone (non-Lambda) SQS polling consumer's message pipeline a
/// <c>StartUp</c> configured, without a running queue or any AWS credentials. Built by
/// <see cref="BenzeneTestHostExtensions.BuildSqsConsumerHost{TStartUp}"/>; push a message through it
/// with <see cref="HandleAsync(Message)"/> or the same-shaped <see cref="SendSqsAsync(Message)"/>
/// (build one from a <c>MessageBuilder</c> via <c>AsSqsMessage()</c>).
/// </summary>
public sealed class SqsConsumerBenzeneTestHost : IDisposable
{
    private readonly SqsConsumerApplication _application;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    /// <summary>Initializes a new instance of the <see cref="SqsConsumerBenzeneTestHost"/> class.</summary>
    /// <param name="application">The built SQS consumer application.</param>
    /// <param name="serviceResolverFactory">The resolver factory the application runs each message against.</param>
    public SqsConsumerBenzeneTestHost(SqsConsumerApplication application, IServiceResolverFactory serviceResolverFactory)
    {
        _application = application;
        _serviceResolverFactory = serviceResolverFactory;
    }

    /// <summary>
    /// Runs a single message through the pipeline exactly as <c>SqsConsumer</c> would for a poll batch
    /// of one, returning the batch outcome (successful/failed split) the worker uses to decide what to
    /// delete - assert on it, or on whatever a faked outbound client captured.
    /// </summary>
    /// <param name="message">The message to handle (build one via <c>MessageBuilder.Create(...).AsSqsMessage()</c>).</param>
    /// <returns>The batch outcome for the single message.</returns>
    public Task<SqsConsumerBatchResult> HandleAsync(Message message)
    {
        return _application.HandleAsync(new ReceiveMessageResponse
        {
            Messages = new List<Message> { message }
        }, _serviceResolverFactory);
    }

    /// <summary>
    /// Dispatches a message through the consumer pipeline. Verb-parallel alias for
    /// <see cref="HandleAsync(Message)"/>, matching the <c>Send*Async</c> shape the Lambda/GCP hosts use.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    /// <returns>The batch outcome for the single message.</returns>
    public Task<SqsConsumerBatchResult> SendSqsAsync(Message message)
    {
        return HandleAsync(message);
    }

    /// <summary>Disposes the resolver factory (and the service provider it owns).</summary>
    public void Dispose()
    {
        _serviceResolverFactory.Dispose();
    }
}
