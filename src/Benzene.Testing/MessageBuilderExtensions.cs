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
        // HTTP messages use CRLF line endings on the wire (RFC 7230), not the platform newline that
        // AppendLine emits (LF on Linux) - so write "\r\n" explicitly.
        // Start line
        stringBuilder.Append($"{source.Method} {source.Path} HTTP/1.1\r\n");
        // Headers
        foreach (var header in source.Headers)
        {
            stringBuilder.Append($"{header.Key}: {header.Value}\r\n");
        }

        // Empty line to separate headers and body
        stringBuilder.Append("\r\n");
        // Body
        var body = serializer.Serialize(source.Message);
        stringBuilder.Append(body);
        return stringBuilder.ToString();
    }
}
