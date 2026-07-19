using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// Handler middleware that republishes a handler's response as follow-up events, per the pipeline's
/// <see cref="ResponseEventMappings"/>: after the handler runs, every mapping that matches the
/// (topic, result) pair publishes through the registered <see cref="IResponseEventPublisher"/>.
/// Added per pipeline via <c>UseResponseEvents(...)</c>
/// (<see cref="ResponseEventsExtensions.UseResponseEvents"/>).
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request handled by this pipeline.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response produced by the handler.</typeparam>
/// <remarks>
/// The publisher is resolved lazily - only when a mapping actually matched - so pipelines whose
/// messages never match pay a dictionary-lookup, not a DI resolution. Publish failures follow the
/// pipeline's <see cref="PublishFailureMode"/>: <see cref="PublishFailureMode.FailMessage"/>
/// replaces the response with an <c>UnexpectedError</c> (and stops publishing further matches) so
/// the transport nacks/redelivers; <see cref="PublishFailureMode.LogAndContinue"/> logs and keeps
/// going.
/// </remarks>
public class ResponseEventsMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>
{
    private readonly ResponseEventMappings _mappings;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseEventsMiddleware{TRequest,TResponse}"/> class.
    /// </summary>
    /// <param name="mappings">The pipeline's mapping set and failure policy.</param>
    /// <param name="serviceResolver">The current message's scoped resolver, used to lazily resolve the publisher (and logger).</param>
    public ResponseEventsMiddleware(ResponseEventMappings mappings, IServiceResolver serviceResolver)
    {
        _mappings = mappings;
        _serviceResolver = serviceResolver;
    }

    /// <inheritdoc />
    public string Name => "ResponseEvents";

    /// <summary>
    /// Runs the rest of the handler pipeline, then publishes an event for every mapping that
    /// matches the handler's response.
    /// </summary>
    /// <param name="context">The current handler invocation's context.</param>
    /// <param name="next">The rest of the handler pipeline (ending in the handler itself).</param>
    public async Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
    {
        await next();

        var response = context.Response;
        if (response == null)
        {
            return;
        }

        var publications = _mappings.Resolve(context.Topic, response);
        if (publications.Count == 0)
        {
            return;
        }

        var publisher = _serviceResolver.GetService<IResponseEventPublisher>();
        foreach (var publication in publications)
        {
            string failureReason;
            Exception? exception = null;
            try
            {
                var publishResult = await publisher.PublishAsync(publication.EventTopic, publication.Payload);
                if (publishResult.IsSuccessful)
                {
                    continue;
                }

                failureReason = $"publish returned status '{publishResult.Status}'";
            }
            catch (Exception ex)
            {
                exception = ex;
                failureReason = ex.Message;
            }

            var logger = _serviceResolver.TryGetService<ILogger<ResponseEventsMiddleware<TRequest, TResponse>>>();
            if (_mappings.PublishFailureMode == PublishFailureMode.FailMessage)
            {
                logger?.LogError(exception, "Failed to publish response event {EventTopic} for {SourceTopic}: {Reason}",
                    publication.EventTopic, context.Topic.Id, failureReason);
                context.Response = BenzeneResult.UnexpectedError<TResponse>(
                    $"Failed to publish response event '{publication.EventTopic}': {failureReason}");
                return;
            }

            logger?.LogWarning(exception, "Failed to publish response event {EventTopic} for {SourceTopic}: {Reason}",
                publication.EventTopic, context.Topic.Id, failureReason);
        }
    }
}
