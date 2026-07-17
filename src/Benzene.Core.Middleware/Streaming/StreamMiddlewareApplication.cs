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

/// <summary>
/// The result-producing sibling of <see cref="StreamMiddlewareApplication{TEvent,TItem}"/> — for
/// transports whose event source mapping reads a response back (e.g. AWS Kinesis/DynamoDB Streams'
/// <c>ReportBatchItemFailures</c>), mirroring how <see cref="MiddlewareApplication{TEvent,TContext}"/>
/// has a <see cref="MiddlewareApplication{TEvent,TContext,TResult}"/> sibling for the same reason.
/// </summary>
/// <typeparam name="TEvent">The raw runtime event type (e.g. <c>KinesisEvent</c>).</typeparam>
/// <typeparam name="TItem">The type of item the event is projected into.</typeparam>
/// <typeparam name="TResult">The type of result read back from the processed <see cref="StreamContext{TItem}"/>.</typeparam>
public class StreamMiddlewareApplication<TEvent, TItem, TResult>(
    IMiddlewarePipeline<StreamContext<TItem>> pipeline,
    Func<TEvent, StreamContext<TItem>> mapper,
    Func<StreamContext<TItem>, TResult> resultMapper)
    : MiddlewareApplication<TEvent, StreamContext<TItem>, TResult>(pipeline, mapper, resultMapper);
