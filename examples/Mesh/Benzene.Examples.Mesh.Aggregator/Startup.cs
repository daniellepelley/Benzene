using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Ui;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

public class Startup
{
    private static readonly string ArtifactDirectory = Path.Combine(AppContext.BaseDirectory, "mesh-artifacts");

    private static readonly MeshServiceRegistry Registry = new(new[]
    {
        new MeshServiceRegistryEntry("orders-api", "http://localhost:5310/spec?type=benzene", "http://localhost:5310/healthcheck"),
        new MeshServiceRegistryEntry("payments-api", "http://localhost:5311/spec?type=benzene", "http://localhost:5311/healthcheck"),
        new MeshServiceRegistryEntry("shipping-api", "http://localhost:5312/spec?type=benzene", "http://localhost:5312/healthcheck"),
    });

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        Directory.CreateDirectory(ArtifactDirectory);
        services.UsingBenzene(x => x.AddMeshAggregator(Registry, ArtifactDirectory));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        // Serves the aggregator's own generated manifest.json/services/*.json - the real,
        // continuously-refreshed data behind the dashboard below.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(ArtifactDirectory),
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
