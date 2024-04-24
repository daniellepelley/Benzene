using Benzene.Abstractions.Mappers;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public class AspNetMessageBodyMapper : IMessageBodyMapper<AspNetContext>
{
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