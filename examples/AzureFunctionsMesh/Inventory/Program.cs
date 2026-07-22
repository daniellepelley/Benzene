using Benzene.Azure.Function.Core;
using Benzene.Examples.AzureFunctionsMesh.Inventory;
using Microsoft.Extensions.Hosting;

// The isolated-worker Functions host. UseBenzene<StartUp> wires the Benzene pipeline; the source-generated
// trigger classes (see Triggers.cs) forward each event source into it.
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .UseBenzene<StartUp>()
    .Build();

host.Run();
