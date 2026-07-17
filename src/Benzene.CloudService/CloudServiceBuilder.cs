using Benzene.Abstractions.Middleware;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Wire;

namespace Benzene.CloudService;

/// <summary>
/// Configures a Benzene Cloud Service (docs/specification/cloud-service-profile.md). Every setting
/// has a profile-conformant default; overriding one is always allowed (the extension-point rule of
/// design-principles.md §4 is not suspended by the profile), and overrides that step outside the
/// profile are reflected honestly in the service's <see cref="CloudServiceProfileReport"/> rather
/// than refused.
/// </summary>
public interface ICloudServiceBuilder
{
    /// <summary>Sets the service version reported in the descriptor (participates in the contract hash).</summary>
    ICloudServiceBuilder WithServiceVersion(string serviceVersion);

    /// <summary>Sets the instance id reported in the descriptor, traces, and heartbeats. Defaults to a generated per-process id.</summary>
    ICloudServiceBuilder WithInstanceId(string instanceId);

    /// <summary>
    /// Sets the placement reported in the descriptor. Per mesh.md §2, only set a region the platform
    /// actually documents - never guess. When not set, placement defaults to <c>self-hosted</c>.
    /// </summary>
    ICloudServiceBuilder WithPlacement(string cloud, string? region = null);

    /// <summary>
    /// Sets the collector's envelope URL, enabling the outbound mesh feeds (profile R6):
    /// <c>mesh:register</c> on startup, <c>mesh:heartbeat</c>, and the <c>mesh:traces</c> feed.
    /// Without a collector the service still serves its descriptor on the reserved <c>mesh</c>
    /// topic, but the outbound feeds have nowhere to go and R6 is reported as missing.
    /// </summary>
    ICloudServiceBuilder WithCollector(string collectorEnvelopeUrl);

    /// <summary>Adds health checks run for the reserved <c>healthcheck</c> topic and included in heartbeats (profile R3).</summary>
    ICloudServiceBuilder WithHealthChecks(params IHealthCheck[] healthChecks);

    /// <summary>
    /// Registers the message handler types the service serves (profile R2). When given, the
    /// descriptor is derived eagerly at wire-up and mesh registration starts immediately; when
    /// omitted, handlers come from the container's existing registrations and the descriptor is
    /// derived on the first invocation instead.
    /// </summary>
    ICloudServiceBuilder WithHandlers(params Type[] handlerTypes);

    /// <summary>
    /// Adds custom middleware to the wire-envelope pipeline, inside the profile's own middleware
    /// (trace, health, descriptor) and before the message router. Custom middleware for the outer
    /// HTTP pipeline needs no hook: add it to that pipeline before calling
    /// <c>UseBenzeneCloudService</c>.
    /// </summary>
    ICloudServiceBuilder WithMiddleware(Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> configure);

    /// <summary>Relocates the wire-envelope endpoint from <see cref="CloudServicePaths.Invoke"/> (flags R7 in the profile report).</summary>
    ICloudServiceBuilder WithInvokePath(string path);

    /// <summary>Relocates the spec endpoint from <see cref="CloudServicePaths.Spec"/> (flags R7 in the profile report).</summary>
    ICloudServiceBuilder WithSpecPath(string path);

    /// <summary>Relocates the health endpoint from <see cref="CloudServicePaths.Health"/> (flags R7 in the profile report).</summary>
    ICloudServiceBuilder WithHealthPath(string path);

    /// <summary>Replaces the default HTTP trace exporter (an extension point, not a profile deviation).</summary>
    ICloudServiceBuilder WithTraceExporter(IMeshTraceExporter traceExporter);

    /// <summary>
    /// Runs <paramref name="callback"/> with the wiring-time <see cref="CloudServiceProfileReport"/> once
    /// it has been evaluated, for services (or tests) that want to inspect the self-assessment at wire-up
    /// without resolving it back out of DI - e.g. logging a startup warning when the profile isn't fully
    /// met. The same report is also registered in DI and stamped on the descriptor (mesh.md §2's
    /// <c>profile</c> field); this is a convenience for the wire-up call site itself.
    /// </summary>
    ICloudServiceBuilder WithProfileReport(Action<CloudServiceProfileReport> callback);

    /// <summary>
    /// Declines the mesh feeds entirely: no reserved <c>mesh</c> topic, no trace middleware, no
    /// registration or heartbeats. The service remains a working Benzene Core service and the
    /// profile report marks R6 and R8 as missing - use this for a deliberate opt-out, not as a
    /// substitute for simply not configuring a collector.
    /// </summary>
    ICloudServiceBuilder WithoutMesh();
}

internal sealed class CloudServiceBuilder : ICloudServiceBuilder
{
    public CloudServiceBuilder(string serviceName)
    {
        ServiceName = serviceName;
    }

    public string ServiceName { get; }
    public string? ServiceVersion { get; private set; }
    public string? InstanceId { get; private set; }
    public MeshPlacement? Placement { get; private set; }
    public string? CollectorEnvelopeUrl { get; private set; }
    public List<IHealthCheck> HealthChecks { get; } = new();
    public Type[]? HandlerTypes { get; private set; }
    public Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>>? EnvelopeMiddleware { get; private set; }
    public string InvokePath { get; private set; } = CloudServicePaths.Invoke;
    public string SpecPath { get; private set; } = CloudServicePaths.Spec;
    public string HealthPath { get; private set; } = CloudServicePaths.Health;
    public IMeshTraceExporter? TraceExporter { get; private set; }
    public bool MeshEnabled { get; private set; } = true;
    public Action<CloudServiceProfileReport>? ProfileReportCallback { get; private set; }

    public bool UsesDefaultPaths =>
        InvokePath == CloudServicePaths.Invoke &&
        SpecPath == CloudServicePaths.Spec &&
        HealthPath == CloudServicePaths.Health;

    public MeshServiceInfo BuildServiceInfo()
    {
        return new MeshServiceInfo(
            ServiceName,
            ServiceVersion,
            InstanceId ?? $"{ServiceName}-{Guid.NewGuid().ToString("N")[..4]}",
            binding: "http",
            placement: Placement);
    }

    public ICloudServiceBuilder WithServiceVersion(string serviceVersion)
    {
        ServiceVersion = serviceVersion;
        return this;
    }

    public ICloudServiceBuilder WithInstanceId(string instanceId)
    {
        InstanceId = instanceId;
        return this;
    }

    public ICloudServiceBuilder WithPlacement(string cloud, string? region = null)
    {
        Placement = new MeshPlacement { Cloud = cloud, Region = region };
        return this;
    }

    public ICloudServiceBuilder WithCollector(string collectorEnvelopeUrl)
    {
        CollectorEnvelopeUrl = collectorEnvelopeUrl;
        return this;
    }

    public ICloudServiceBuilder WithHealthChecks(params IHealthCheck[] healthChecks)
    {
        HealthChecks.AddRange(healthChecks);
        return this;
    }

    public ICloudServiceBuilder WithHandlers(params Type[] handlerTypes)
    {
        HandlerTypes = HandlerTypes == null ? handlerTypes : HandlerTypes.Concat(handlerTypes).ToArray();
        return this;
    }

    public ICloudServiceBuilder WithMiddleware(Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> configure)
    {
        EnvelopeMiddleware += configure;
        return this;
    }

    public ICloudServiceBuilder WithInvokePath(string path)
    {
        InvokePath = path;
        return this;
    }

    public ICloudServiceBuilder WithSpecPath(string path)
    {
        SpecPath = path;
        return this;
    }

    public ICloudServiceBuilder WithHealthPath(string path)
    {
        HealthPath = path;
        return this;
    }

    public ICloudServiceBuilder WithTraceExporter(IMeshTraceExporter traceExporter)
    {
        TraceExporter = traceExporter;
        return this;
    }

    public ICloudServiceBuilder WithoutMesh()
    {
        MeshEnabled = false;
        return this;
    }

    public ICloudServiceBuilder WithProfileReport(Action<CloudServiceProfileReport> callback)
    {
        ProfileReportCallback += callback;
        return this;
    }
}
