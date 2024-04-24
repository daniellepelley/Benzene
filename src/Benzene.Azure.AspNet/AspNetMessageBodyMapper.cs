using Benzene.Abstractions.Mappers;
using Microsoft.AspNetCore.Http;

namespace Benzene.Azure.AspNet;

public class AspNetMessageBodyMapper : IMessageBodyMapper<AspNetContext>
{
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
