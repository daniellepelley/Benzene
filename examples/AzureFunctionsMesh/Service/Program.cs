using Benzene.Azure.Function.Core;
using Benzene.Examples.AzureFunctionsMesh.Service;
using Microsoft.Extensions.Hosting;

// The isolated-worker Functions host. UseBenzene<StartUp> wires the Benzene pipeline; the catch-all
// HttpFunction forwards every request into it, so the whole Cloud Service Profile (/benzene/spec,
// /benzene/health, /benzene/invoke) is served by one HTTP-triggered function.
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .UseBenzene<StartUp>()
    .Build();

host.Run();
