using Benzene.HostedService;
using BenzeneStarter;

// A plain .NET generic host - no ASP.NET/Kestrel. Benzene owns the process and hosts a lightweight
// HttpListener-based HTTP server as a background IHostedService (see StartUp.Configure). This is the
// self-hosted counterpart of the `benzene.asp` template: the same handlers, no web framework.
IHost host = Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build();

await host.RunAsync();
