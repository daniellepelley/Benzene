using System.Text;
using Benzene.Abstractions.Serialization;

namespace Benzene.Testing;

public static class MessageBuilderExtensions
{
    public static MessageBuilder<T> WithHeaders<T>(this MessageBuilder<T> source, IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            source.WithHeader(header.Key, header.Value);
        }

        return source;
    }

    public static string AsRawHttpRequest<T>(this HttpBuilder<T> source, ISerializer serializer)
        where T : class
    {
        var stringBuilder = new StringBuilder();
        // Start line
        stringBuilder.AppendLine($"{source.Method} {source.Path} HTTP/1.1");
        // Headers
        foreach (var header in source.Headers)
        {
            stringBuilder.AppendLine($"{header.Key}: {header.Value}");
        }

        // Empty line to separate headers and body
        stringBuilder.AppendLine();
        // Body
        var body = serializer.Serialize(source.Message);
        stringBuilder.Append(body);
        return stringBuilder.ToString();
    }
}
