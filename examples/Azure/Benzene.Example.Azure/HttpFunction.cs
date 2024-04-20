using System.Threading.Tasks;
using Benzene.Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Benzene.Example.Azure;

public class HttpFunction
{
    private readonly IAzureApp _app;

    public HttpFunction(IAzureApp app)
    {
        _app = app;
    }

    [FunctionName("orders")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "{*restOfPath}")] HttpRequest req)
    {
        return await _app.HandleHttpRequest(req);
    }
}