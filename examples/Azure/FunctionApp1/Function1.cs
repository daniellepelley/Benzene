using System.Net;
using Benzene.Azure.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp1
{
    public class HttpFunction
    {
        private readonly ILogger _logger;
        private IAzureFunctionApp _app;

        public HttpFunction(ILoggerFactory loggerFactory, IAzureFunctionApp app)
        {
            _app = app;
            _logger = loggerFactory.CreateLogger<HttpFunction>();
        }

        [Function("orders")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
