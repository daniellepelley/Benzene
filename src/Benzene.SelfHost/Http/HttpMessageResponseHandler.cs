using Benzene.Abstractions.Response;
using Benzene.Core.Serialization;
using Benzene.Results;

namespace Benzene.SelfHost.Http;

public class HttpMessageResponseHandler : ISyncResponseHandler<HttpContext>
{
    public void HandleAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(context.MessageResult.Topic))
        {
            context.Response = new HttpResponse { StatusCode = 404 };
        }

        if (!string.IsNullOrEmpty(context.MessageResult.Topic))
        {
            var payload = context.MessageResult.Payload;
            var status = context.MessageResult.Status.AsHttpStatusCode();

            context.Response = new HttpResponse
            {
                Body = payload != null ? new JsonSerializer().Serialize(payload) : null,
                StatusCode = Convert.ToInt32(status)
            };
        }
    }
}
