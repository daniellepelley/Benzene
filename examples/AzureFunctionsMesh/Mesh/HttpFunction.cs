using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Examples.AzureFunctionsMesh.Mesh;

/// <summary>
/// The catch-all HTTP trigger for the mesh. It forwards every request into the built Benzene app, which
/// serves the Mesh UI (<c>/mesh-ui</c>), the catalog artifacts (<c>manifest.json</c>,
/// <c>services/*.json</c>, …), and the on-demand <c>POST /mesh/refresh</c> endpoint. Anonymous so the UI
/// is publicly reachable; <c>host.json</c> clears the <c>/api</c> route prefix so those paths sit at the
/// root the UI's relative fetches expect.
/// </summary>
public class HttpFunction
{
    private readonly IAzureFunctionApp _app;

    public HttpFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("mesh-http")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "options", Route = "{*restOfPath}")] HttpRequest req)
    {
        return await _app.HandleHttpRequest(req);
    }
}
