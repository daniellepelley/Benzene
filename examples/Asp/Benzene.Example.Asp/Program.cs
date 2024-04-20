using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Benzene.Example.Asp;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseKestrel()
                    .ConfigureAppConfiguration(x => x
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("config.json")
                        .AddEnvironmentVariables()
                        .Build()
                    )
                    .UseStartup<Startup>();
            });
}