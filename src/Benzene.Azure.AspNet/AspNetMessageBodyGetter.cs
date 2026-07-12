using Benzene.Abstractions.Messages.Mappers;
using Microsoft.AspNetCore.Http;

namespace Benzene.Azure.AspNet;

/// <summary>
/// Extracts the message body by reading the HTTP request body stream as text.
/// </summary>
public class AspNetMessageBodyGetter : IMessageBodyGetter<AspNetContext>
{
    /// <summary>
    /// Reads the HTTP request body as a string.
    /// </summary>
    /// <param name="context">The HTTP context to extract the body from.</param>
    /// <returns>The request body as a string, or <c>null</c> if reading the body throws.</returns>
    public string? GetBody(AspNetContext context)
    {
        return StreamToString(context.HttpRequest);
    }

    private static string StreamToString(HttpRequest request)
    {
        try
        {
            using var sr = new StreamReader(request.Body);
            var json = sr.ReadToEndAsync().Result;
            // request.Body.Position = 0;
            return json;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
