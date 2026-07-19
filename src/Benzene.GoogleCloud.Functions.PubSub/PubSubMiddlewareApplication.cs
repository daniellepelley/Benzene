using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Microsoft.Extensions.Logging;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Runs a single Pub/Sub message through the middleware pipeline, applying <see cref="PubSubOptions"/>
/// to decide whether the message's exception or failure result is contained (logged, doesn't fail
/// the invocation) or left to cascade and fail it. Unlike AWS/Azure's batch-oriented trigger
/// applications, there is no fan-out here - Cloud Functions delivers exactly one Pub/Sub message per
/// invocation.
/// </summary>
public class PubSubMiddlewareApplication : IMiddlewareApplication<MessagePublishedData>
{
    private readonly IMiddlewarePipeline<PubSubContext> _pipeline;
    private readonly PubSubOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubMiddlewareApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Pub/Sub middleware pipeline to run the message through.</param>
    /// <param name="options">
    /// Configures how a handler's exceptions and failure results are handled. Defaults to a new
    /// <see cref="PubSubOptions"/> instance (both <see cref="PubSubOptions.CatchExceptions"/> and
    /// <see cref="PubSubOptions.RaiseOnFailureStatus"/> off) if omitted.
    /// </param>
    public PubSubMiddlewareApplication(IMiddlewarePipeline<PubSubContext> pipeline, PubSubOptions? options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<PubSubContext>(TransportNames.PubSub, pipeline);
        _options = options ?? new PubSubOptions();
    }

    /// <summary>
    /// Handles the Pub/Sub message delivered for this invocation.
    /// </summary>
    /// <param name="event">The Pub/Sub CloudEvent payload.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process the message.</param>
    public async Task HandleAsync(MessagePublishedData @event, IServiceResolverFactory serviceResolverFactory)
    {
        var context = new PubSubContext(@event);

        try
        {
            using (var scope = serviceResolverFactory.CreateScope())
            {
                await _pipeline.HandleAsync(context, scope);
            }

            if (_options.RaiseOnFailureStatus && context.MessageResult?.IsSuccessful == false)
            {
                throw new PubSubMessageProcessingException(context.Message.MessageId);
            }
        }
        catch (Exception ex) when (_options.CatchExceptions)
        {
            using (var loggingScope = serviceResolverFactory.CreateScope())
            {
                loggingScope.GetService<ILogger<PubSubMiddlewareApplication>>()
                    .LogError(ex, "Processing Pub/Sub message {messageId} failed", context.Message.MessageId);
            }
        }
    }
}
