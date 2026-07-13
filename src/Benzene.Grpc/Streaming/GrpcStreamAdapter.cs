using System.Reflection;
using Benzene.Grpc.Serialization;
using Grpc.Core;

namespace Benzene.Grpc.Streaming;

/// <summary>
/// Bridges gRPC's streaming primitives (<see cref="IAsyncStreamReader{T}"/>/<see cref="IServerStreamWriter{T}"/>)
/// and <see cref="IAsyncEnumerable{T}"/>, the shape Benzene message handlers use for stream request/response
/// items, converting per item via <see cref="IGrpcMessageAdapter"/> when a handler declares a POCO item type.
/// </summary>
internal static class GrpcStreamAdapter
{
    private static readonly MethodInfo ConvertMethodDefinition = typeof(GrpcStreamAdapter)
        .GetMethod(nameof(Convert), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>Adapts an inbound gRPC request stream to an <see cref="IAsyncEnumerable{T}"/>.</summary>
    internal static async IAsyncEnumerable<T> ReadAll<T>(IAsyncStreamReader<T> reader, CancellationToken cancellationToken)
    {
        while (await reader.MoveNext(cancellationToken))
        {
            yield return reader.Current;
        }
    }

    /// <summary>Writes every item of <paramref name="source"/> to an outbound gRPC response stream.</summary>
    internal static async Task WriteAll<T>(IAsyncEnumerable<T> source, IServerStreamWriter<T> writer, CancellationToken cancellationToken)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            await writer.WriteAsync(item);
        }
    }

    /// <summary>
    /// If <paramref name="source"/> is itself an <see cref="IAsyncEnumerable{T}"/> and <paramref name="targetType"/>
    /// is <c>IAsyncEnumerable&lt;TOut&gt;</c> for some <c>TOut</c>, returns a lazily-converting
    /// <see cref="IAsyncEnumerable{TOut}"/> that maps each item through <paramref name="adapter"/>. Returns
    /// <c>null</c> if either side isn't a stream (e.g. this isn't a streaming request/response at all).
    /// </summary>
    /// <param name="isResponseDirection">
    /// <c>false</c> for inbound request-stream items (converted via <see cref="IGrpcMessageAdapter.ConvertRequest{TRequest}"/>,
    /// e.g. protobuf-to-POCO); <c>true</c> for outbound response-stream items (converted via
    /// <see cref="IGrpcMessageAdapter.ConvertResponse{TResponse}"/>, e.g. POCO-to-protobuf).
    /// </param>
    internal static object? TryConvertStream(object? source, Type targetType, IGrpcMessageAdapter adapter, bool isResponseDirection, CancellationToken cancellationToken)
    {
        var targetItemType = GetAsyncEnumerableItemType(targetType);
        var sourceItemType = GetAsyncEnumerableItemType(source?.GetType());

        if (targetItemType == null || sourceItemType == null)
        {
            return null;
        }

        return ConvertMethodDefinition
            .MakeGenericMethod(sourceItemType, targetItemType)
            .Invoke(null, new object?[] { source, adapter, isResponseDirection, cancellationToken });
    }

    private static async IAsyncEnumerable<TOut> Convert<TIn, TOut>(IAsyncEnumerable<TIn> source, IGrpcMessageAdapter adapter, bool isResponseDirection, CancellationToken cancellationToken)
        where TOut : class
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            yield return isResponseDirection ? adapter.ConvertResponse<TOut>(item) : adapter.ConvertRequest<TOut>(item)!;
        }
    }

    private static Type? GetAsyncEnumerableItemType(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        return type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            ?.GetGenericArguments()[0];
    }
}
