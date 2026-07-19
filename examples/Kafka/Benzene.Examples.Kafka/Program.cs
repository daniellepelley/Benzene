using Benzene.Examples.Kafka;
using Benzene.HostedService;

IHost host = Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build();

await host.RunAsync();
