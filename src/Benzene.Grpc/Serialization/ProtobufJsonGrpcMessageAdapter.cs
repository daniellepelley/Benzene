using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Benzene.Core.Exceptions;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Benzene.Grpc.Serialization;

/// <summary>
/// Bridges Benzene message handler payloads and protobuf messages. Values that already are the
/// target protobuf/POCO type pass through untouched; everything else is converted via protobuf's
/// own JSON representation (<see cref="JsonFormatter"/>/<see cref="JsonParser"/>), not System.Text.Json,
/// so protobuf-specific constructs (well-known types, enums, oneofs) round-trip correctly.
/// </summary>
public class ProtobufJsonGrpcMessageAdapter : IGrpcMessageAdapter
{
    private static readonly ConcurrentDictionary<Type, MessageDescriptor> DescriptorsByType = new();

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TRequest? ConvertRequest<TRequest>(object? protobufMessage)
    {
        switch (protobufMessage)
        {
            case null:
                return default;
            case TRequest direct:
                return direct;
            case IMessage message:
                return JsonSerializer.Deserialize<TRequest>(JsonFormatter.Default.Format(message), DeserializeOptions);
            default:
                return JsonSerializer.Deserialize<TRequest>(JsonSerializer.Serialize(protobufMessage), DeserializeOptions);
        }
    }

    public TResponse ConvertResponse<TResponse>(object? payload)
    {
        if (payload is TResponse direct)
        {
            return direct;
        }

        if (payload == null)
        {
            throw new BenzeneException($"Cannot convert a null payload to {typeof(TResponse).Name}.");
        }

        var descriptor = GetDescriptor(typeof(TResponse));
        var json = JsonSerializer.Serialize(payload, SerializeOptions);

        if (JsonParser.Default.Parse(json, descriptor) is not TResponse parsed)
        {
            throw new BenzeneException($"Failed to convert payload to {typeof(TResponse).Name}.");
        }

        return parsed;
    }

    private static MessageDescriptor GetDescriptor(Type type)
    {
        return DescriptorsByType.GetOrAdd(type, static t =>
        {
            var property = t.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
            if (property?.GetValue(null) is MessageDescriptor descriptor)
            {
                return descriptor;
            }

            throw new BenzeneException($"Type {t.Name} is not a protobuf message; it does not expose a static Descriptor property.");
        });
    }
}
