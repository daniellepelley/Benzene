using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Examples.AzureFunctionsMesh.Service;

/// <summary>
/// The single catch-all HTTP trigger. It forwards every request into the built Benzene app, which
/// routes it to the right handler (or Cloud Service Profile surface) by method + path — so
/// <c>GET /benzene/spec</c>, <c>GET /benzene/health</c>, <c>POST /benzene/invoke</c>, and the domain's
/// own <c>[HttpEndpoint]</c> routes are all served here. Anonymous so the mesh (which carries no
/// function key) can interrogate the spec/health surfaces; <c>host.json</c> clears the <c>/api</c> route
/// prefix so the paths sit at the root the mesh expects.
/// </summary>
public class HttpFunction
{
    private readonly IAzureFunctionApp _app;

    public HttpFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("service")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options", Route = "{*restOfPath}")] HttpRequest req)
    {
        return await _app.HandleHttpRequest(req);
    }
}
