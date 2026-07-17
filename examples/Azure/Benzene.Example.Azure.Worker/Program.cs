using Benzene.Example.Azure.Worker;
using Benzene.HostedService;

// A plain .NET Worker Service - no Azure Functions runtime. Benzene owns the process and hosts the
// Service Bus and Event Hub consumers as background IHostedServices (see StartUp.Configure).
//
// IMPORTANT: this UseBenzene<StartUp>() is Benzene.HostedService's - it registers the workers as an
// IHostedService. Benzene.Azure.Function.Core declares an identically-named IHostBuilder extension
// that registers an Azure Functions app instead, so keep only `using Benzene.HostedService;` here.
IHost host = Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build();

await host.RunAsync();
