using System.Text;
using Newtonsoft.Json;

namespace Benzene.Tools;

public static class MessageBuilderExtensions
{
    public static MessageBuilder WithHeaders(this MessageBuilder source, IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            source.WithHeader(header.Key, header.Value);
        }

        return source;
    }

    public static string AsRawHttpRequest(this HttpBuilder source)
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
        var body = JsonConvert.SerializeObject(source.Message);
        stringBuilder.Append(body);
        return stringBuilder.ToString();
    }
}
