using System.Text;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost.Http;

public class HttpLambdaHandler : MiddlewareRouter<HttpRequest, SelfHostContext>
{
    private readonly HttpApplication _httpApplication;

    public HttpLambdaHandler(IMiddlewarePipeline<HttpContext> pipeline,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        _httpApplication = new HttpApplication(pipeline);
    }

    protected override bool CanHandle(HttpRequest request)
    {
        return request?.Method != null;
    }

    protected override async Task HandleFunction(HttpRequest request, SelfHostContext context, IServiceResolver serviceResolver)
    {
        var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        setCurrentTransport.SetTransport("http");
        var response = await HandleAsync(request, serviceResolver);
        context.Response = ConvertToRawHttpResponse(response);
    }

    private async Task<HttpResponse> HandleAsync(HttpRequest request, IServiceResolver serviceResolver)
    {
        try
        {
            return await _httpApplication.HandleAsync(request, serviceResolver);
        }
        catch (Exception ex)
        {
            return new HttpResponse
            {
                StatusCode = 500,
                Body = ex.ToString()
            };
        }
    }

    private string ConvertToRawHttpResponse(HttpResponse response)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"HTTP/1.1 {response.StatusCode}");

        if (response.Headers != null)
        {
            foreach (var header in response.Headers)
            {
                stringBuilder.AppendLine($"{header.Key}: {header.Value}");
            }
        }

        if (!string.IsNullOrEmpty(response.Body))
        {
            stringBuilder.AppendLine($"content-length: {response.Body.Length}");
            stringBuilder.AppendLine();
            stringBuilder.Append(response.Body);
        }
        else
        {
            stringBuilder.AppendLine($"content-length: 0");
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }


    protected override HttpRequest TryExtractRequest(SelfHostContext context)
    {
        return HttpRequestParser.Parse(context.Request);
    }
}
