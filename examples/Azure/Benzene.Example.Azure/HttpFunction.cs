using System.Threading.Tasks;
using Benzene.Azure.AspNet;
using Benzene.Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Benzene.Example.Azure;

public class HttpFunction
{
    private readonly IAzureFunctionApp _app;

    public HttpFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [FunctionName("orders")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options", Route = "{*restOfPath}")] HttpRequest req)
    {
        return await _app.HandleHttpRequest(req);
    }
}