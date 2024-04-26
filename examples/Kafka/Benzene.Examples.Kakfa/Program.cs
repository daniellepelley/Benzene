using Benzene.Examples.Kakfa;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<StartUp>();
    })
    .Build();

await host.RunAsync();
