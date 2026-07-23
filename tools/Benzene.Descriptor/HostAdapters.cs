using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Descriptor;

/// <summary>The built service, ready to introspect: a resolver over its container.</summary>
internal sealed record HostBuild(IServiceResolver Resolver, bool TransportsResolved, string HostName);

/// <summary>
/// Constructs a built Benzene service far enough to introspect it — running its registration but never
/// the run/listen step. The <b>core descriptor is cloud-agnostic</b> (it comes from host-neutral
/// <c>ConfigureServices</c>); a host adapter only exists to run the host-specific <c>Configure</c> so
/// the inbound transport-name list (and validation-enriched schemas) can be read. New clouds are new
/// adapters — nothing else changes.
/// </summary>
internal interface IHostAdapter
{
    string Name { get; }
    bool CanHandle(Assembly serviceAssembly);
    HostBuild Build(Type startUpType);
}

internal static class HostAdapters
{
    // Ordered: the most specific host that matches wins; the neutral adapter always matches last.
    public static IReadOnlyList<IHostAdapter> All { get; } = new IHostAdapter[]
    {
        new AwsLambdaHostAdapter(),
        new NeutralHostAdapter(),
    };

    public static IHostAdapter Select(Assembly serviceAssembly)
        => All.First(a => a.CanHandle(serviceAssembly));

    // Runs ConfigureServices and hands back a resolver over the container. Host-neutral; no host types.
    internal static (IServiceCollection Services, BenzeneStartUp StartUp, IConfiguration Config) Register(Type startUpType)
    {
        var startUp = (BenzeneStartUp)Activator.CreateInstance(startUpType)!;
        var config = startUp.GetConfiguration();
        var services = new ServiceCollection();
        services.AddLogging();
        startUp.ConfigureServices(services, config);
        return (services, startUp, config);
    }

    internal static IServiceResolver Resolver(IServiceCollection services)
        => new MicrosoftServiceResolverFactory(services).CreateScope();
}

/// <summary>
/// The cloud-agnostic fallback: <c>ConfigureServices</c> only. Yields the full logical contract
/// (consumes, produces, HTTP routes, base schemas, outbound transport kinds) for ANY host, with no
/// cloud coupling. The inbound transport-name list is not available (that needs the host's Configure),
/// so <see cref="HostBuild.TransportsResolved"/> is false.
/// </summary>
internal sealed class NeutralHostAdapter : IHostAdapter
{
    public string Name => "neutral";
    public bool CanHandle(Assembly serviceAssembly) => true;

    public HostBuild Build(Type startUpType)
    {
        var (services, _, _) = HostAdapters.Register(startUpType);
        return new HostBuild(HostAdapters.Resolver(services), TransportsResolved: false, HostName: Name);
    }
}

/// <summary>
/// The AWS Lambda host adapter: runs <c>ConfigureServices</c> + <c>Configure</c> against an
/// <see cref="AwsLambdaApplicationBuilder"/>, exactly as <c>AwsLambdaHost&lt;StartUp&gt;</c>'s
/// constructor does — but without the Lambda runtime. This populates the inbound transports and the
/// validation-enriched schemas. It is the only cloud-coupled piece; a future Azure/GCP adapter is the
/// same shape against that host's application builder.
/// </summary>
internal sealed class AwsLambdaHostAdapter : IHostAdapter
{
    public string Name => "aws-lambda";

    public bool CanHandle(Assembly serviceAssembly)
        => serviceAssembly.GetReferencedAssemblies()
            .Any(a => a.Name == "Benzene.Aws.Lambda.Core");

    public HostBuild Build(Type startUpType)
    {
        var (services, startUp, config) = HostAdapters.Register(startUpType);
        var container = new MicrosoftBenzeneServiceContainer(services);
        var eventPipeline = new MiddlewarePipelineBuilder<AwsEventStreamContext>(container);
        startUp.Configure(new AwsLambdaApplicationBuilder(eventPipeline, container), config);
        eventPipeline.Build();
        return new HostBuild(HostAdapters.Resolver(services), TransportsResolved: true, HostName: Name);
    }
}
