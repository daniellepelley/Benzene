using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// An entry-point application that presents a whole runtime event (a batch/stream) to the pipeline as
/// a <em>single</em> <see cref="StreamContext{TItem}"/> and runs the pipeline once — fanning
/// <em>in</em>. Contrast <see cref="MiddlewareMultiApplication{TEvent,TContext}"/>, which fans a batch
/// <em>out</em> into one context per item processed concurrently.
/// </summary>
/// <typeparam name="TEvent">The raw runtime event type (e.g. <c>EventData[]</c>).</typeparam>
/// <typeparam name="TItem">The type of item the event is projected into.</typeparam>
public class StreamMiddlewareApplication<TEvent, TItem>(
    IMiddlewarePipeline<StreamContext<TItem>> pipeline,
    Func<TEvent, StreamContext<TItem>> mapper)
    : MiddlewareApplication<TEvent, StreamContext<TItem>>(pipeline, mapper);
