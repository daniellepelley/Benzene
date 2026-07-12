using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Azure.EventHub.Function;

/// <summary>
/// Routes an Event Hub event whose body deserializes into a <see cref="BenzeneMessageRequest"/> to the
/// direct-message middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the Event Hub pipeline by <see cref="Extensions.UseBenzeneMessage(IMiddlewarePipelineBuilder{EventHubContext}, Action{IMiddlewarePipelineBuilder{BenzeneMessageContext}})"/>.
/// It only handles the event if its body deserializes into a <see cref="BenzeneMessageRequest"/> with a
/// non-null topic; otherwise it defers to the next middleware.
/// </remarks>
public class BenzeneMessageEventHubHandler : MiddlewareRouter<BenzeneMessageRequest, EventHubContext>
{
    private readonly BenzeneMessageApplication _directMessageApplication;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageEventHubHandler"/> class.
    /// </summary>
    /// <param name="pipeline">The direct-message middleware pipeline to dispatch matching events to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public BenzeneMessageEventHubHandler(
        IMiddlewarePipeline<BenzeneMessageContext> pipeline,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        _serializer = serviceResolver.GetService<ISerializer>();
        _directMessageApplication = new BenzeneMessageApplication(pipeline);
    }

    /// <summary>
    /// Determines whether the deserialized request looks like a direct Benzene message.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the request has a non-null topic; otherwise, false.</returns>
    protected override bool CanHandle(BenzeneMessageRequest request)
    {
        return request?.Topic != null;
    }

    /// <summary>
    /// Handles the event by running it through the direct-message application.
    /// </summary>
    /// <param name="request">The deserialized Benzene message request.</param>
    /// <param name="context">The Event Hub context for this invocation.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(BenzeneMessageRequest request, EventHubContext context, IServiceResolverFactory serviceResolverFactory)
    {
        await _directMessageApplication.HandleAsync(request, serviceResolverFactory);
    }

    /// <summary>
    /// Attempts to deserialize the Event Hub event body into a <see cref="BenzeneMessageRequest"/>.
    /// </summary>
    /// <param name="context">The Event Hub context to extract the request from.</param>
    /// <returns>The deserialized request, or <c>default</c> if deserialization fails.</returns>
    protected override BenzeneMessageRequest TryExtractRequest(EventHubContext context)
    {
        try
        {
            return _serializer.Deserialize<BenzeneMessageRequest>(context.EventData.EventBody.ToString());
        }
        catch
        {
            return default;
        }
    }
}
