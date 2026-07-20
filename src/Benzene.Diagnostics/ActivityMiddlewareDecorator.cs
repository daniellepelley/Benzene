using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Diagnostics;

public class ActivityMiddlewareDecorator<TContext> : IMiddleware<TContext>
{
    private readonly IMiddleware<TContext> _inner;
    private readonly IServiceResolver _serviceResolver;

    public ActivityMiddlewareDecorator(IMiddleware<TContext> inner, IServiceResolver serviceResolver)
    {
        _inner = inner;
        _serviceResolver = serviceResolver;
    }

    public string Name => _inner.Name;

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        using var activity = BenzeneDiagnostics.ActivitySource.StartActivity(Name);
        if (activity is not null)
        {
            Tag(activity, context);
        }

        try
        {
            await _inner.HandleAsync(context, next);
        }
        catch (Exception ex)
        {
            // Without this, a span that threw looks identical to one that succeeded in a trace
            // viewer (Jaeger/Tempo/App Insights) - no error flag, no exception. Marking the span
            // (at every level the exception propagates through) is what lets a trace point at the
            // failing stage. Then rethrow untouched - this only observes, it doesn't handle.
            if (activity is not null)
            {
                activity.AddException(ex);
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            }

            throw;
        }
    }

    private void Tag(Activity activity, TContext context)
    {
        var transport = _serviceResolver.TryGetService<ICurrentTransport>();
        if (transport is not null)
        {
            activity.SetTag("benzene.transport", transport.Name);
        }

        var topic = _serviceResolver.TryGetService<IMessageGetter<TContext>>()?.GetTopic(context);
        if (topic is not null && !string.IsNullOrEmpty(topic.Id))
        {
            activity.SetTag("benzene.topic", topic.Id);
            activity.SetTag("benzene.version", topic.Version);

            var handler = _serviceResolver.TryGetService<IMessageHandlerDefinitionLookUp>()?.FindHandler(topic);
            if (handler is not null)
            {
                activity.SetTag("benzene.handler", handler.HandlerType.Name);
            }
        }
    }
}
