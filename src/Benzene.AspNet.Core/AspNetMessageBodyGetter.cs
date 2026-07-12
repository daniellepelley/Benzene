using Benzene.Abstractions.Messages.Mappers;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Extracts the message body by reading the HTTP request body stream as text.
/// </summary>
public class AspNetMessageBodyGetter : IMessageBodyGetter<AspNetContext>
{
    /// <summary>
    /// Reads the HTTP request body as a string.
    /// </summary>
    /// <param name="context">The HTTP context to extract the body from.</param>
    /// <returns>The request body as a string, or <c>null</c> if the body stream is unavailable or reading it throws.</returns>
    public string GetBody(AspNetContext context)
    {
        return StreamToString(context.HttpContext.Request);
    }

    private static string StreamToString(HttpRequest request)
    {
        try
        {
            if (request.Body == null)
            {
                return null;
            }

            using var sr = new StreamReader(request.Body);
            var json = sr.ReadToEndAsync().Result;
            return json;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
