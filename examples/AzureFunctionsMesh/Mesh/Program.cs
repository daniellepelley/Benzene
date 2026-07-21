using Benzene.Azure.Function.Core;
using Benzene.Examples.AzureFunctionsMesh.Mesh;
using Microsoft.Extensions.Hosting;

// The mesh, hosted purely as Azure Functions: a catch-all HTTP trigger serves the Mesh UI + catalog
// artifacts, and a timer trigger drives the periodic discovery + aggregation pass. Both entry points
// dispatch into the same built Benzene app.
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .UseBenzene<StartUp>()
    .Build();

host.Run();
