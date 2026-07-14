using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Adds streaming (fan-in) Event Hub handling: the whole triggered batch is presented to the pipeline
/// as a single <see cref="StreamContext{TItem}"/> of raw <see cref="EventData"/>, preserving order and
/// enabling windowing/aggregation — the opt-in alternative to <c>UseEventHub(...)</c>, which fans the
/// batch out into one message per event.
/// </summary>
public static class StreamingExtensions
{
    /// <summary>
    /// Adds a streaming Event Hub entry point that runs the pipeline once over the whole batch.
    /// </summary>
    /// <param name="app">The Azure Function app builder.</param>
    /// <param name="action">Configures the stream pipeline (add <c>UseStream(...)</c> etc.).</param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseEventHubStream(this IAzureFunctionAppBuilder app,
        Action<IMiddlewarePipelineBuilder<StreamContext<EventData>>> action)
    {
        var pipeline = app.Create<StreamContext<EventData>>();
        action(pipeline);
        app.Add(serviceResolverFactory => new EntryPointMiddlewareApplication<EventData[]>(
            new StreamMiddlewareApplication<EventData[], EventData>(pipeline.Build(), ToStreamContext),
            serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies streaming Event Hub configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">Configures the stream pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseEventHubStream(this IBenzeneApplicationBuilder app,
        Action<IMiddlewarePipelineBuilder<StreamContext<EventData>>> action)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseEventHubStream(action);
        }

        return app;
    }

    private static StreamContext<EventData> ToStreamContext(EventData[] events)
    {
        return new StreamContext<EventData>(ToAsyncEnumerable(events));
    }

    private static async IAsyncEnumerable<EventData> ToAsyncEnumerable(EventData[] events)
    {
        foreach (var eventData in events)
        {
            yield return eventData;
        }

        await Task.CompletedTask;
    }
}
