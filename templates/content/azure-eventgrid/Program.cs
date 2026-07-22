using Benzene.Azure.Function.Core;
using BenzeneStarter;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .UseBenzene<StartUp>()
    .Build();

host.Run();
