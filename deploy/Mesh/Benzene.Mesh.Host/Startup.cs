using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Aws.Lambda;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Benzene.Mesh.Host;

/// <summary>
/// Config-driven, dockerized Benzene Mesh Aggregator + UI - mirrors
/// <c>examples/Mesh/Benzene.Examples.Mesh.Aggregator/Startup.cs</c>'s wiring shape, but reads its
/// service registry from <see cref="MeshHostConfig"/> (bound from <c>mesh.json</c>/environment
/// variables) instead of a hardcoded static field, and adds a background poll loop since a bare
/// Docker Compose deployment has no external scheduler.
/// </summary>
public class Startup
{
    private readonly MeshHostConfig _config;
    private readonly MeshServiceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="Startup"/> class.</summary>
    /// <param name="configuration">The bound configuration (see <c>Program.cs</c> for how <c>mesh.json</c> is loaded).</param>
    public Startup(IConfiguration configuration)
    {
        _config = configuration.Get<MeshHostConfig>() ?? new MeshHostConfig();
        _registry = new MeshServiceRegistry(_config.Services.Select(s => s.ToEntry()).ToArray());
    }

    /// <summary>Registers services.</summary>
    /// <param name="services">The service collection to register with.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        Directory.CreateDirectory(_config.ArtifactRootDirectory);
        services.AddSingleton(_config);
        services.AddHostedService<MeshPollBackgroundService>();

        services.UsingBenzene(x => x
            .AddMeshAggregator(_registry, _config.ArtifactRootDirectory)
            // Optional per entry - only actually used if a service's Source is AwsLambdaInvoke.
            // Registering it unconditionally is harmless: constructing an AmazonLambdaClient
            // doesn't require valid AWS credentials up front, only an actual Invoke call would.
            .AddMeshLambdaSource());
    }

    /// <summary>Configures the request pipeline.</summary>
    /// <param name="app">The application builder.</param>
    /// <param name="env">The hosting environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        // Serves the aggregator's own generated manifest.json/services/*.json/topology.json - the
        // real, continuously-refreshed data behind the dashboard below.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.GetFullPath(_config.ArtifactRootDirectory)),
            RequestPath = "/artifacts",
        });

        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseMeshUi(path: "/mesh-ui", manifestUrl: "/artifacts/manifest.json")
                .UseMessageHandlers()
            )
        );

        app.UseEndpoints(endpoints => { });
    }
}
