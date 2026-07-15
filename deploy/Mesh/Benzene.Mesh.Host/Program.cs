using Benzene.Mesh.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        // MESH_CONFIG_PATH points at a bind-mounted mesh.json (see deploy/Mesh/README.md) - the
        // primary config path for a multi-service registry. Individual scalars (ArtifactRootDirectory,
        // PollIntervalSeconds) can also be overridden via plain environment variables, since
        // Host.CreateDefaultBuilder already adds those; only the JSON file needs wiring explicitly here.
        var meshConfigPath = Environment.GetEnvironmentVariable("MESH_CONFIG_PATH");
        if (!string.IsNullOrEmpty(meshConfigPath))
        {
            config.AddJsonFile(meshConfigPath, optional: true, reloadOnChange: false);
        }
    })
    .ConfigureWebHost(webBuilder => webBuilder
        .UseKestrel()
        .UseStartup<Startup>())
    .Build()
    .Run();
