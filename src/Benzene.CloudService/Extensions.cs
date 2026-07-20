using System.Threading;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.Http;
using Benzene.Http.BenzeneMessage;
using Benzene.Http.Routing;
using Benzene.Mesh.Wire;
using Benzene.Results;
using Benzene.Schema.OpenApi;

namespace Benzene.CloudService;

/// <summary>
/// The batteries-included setup for a <b>Benzene Cloud Service</b>
/// (docs/specification/cloud-service-profile.md). One call wires every operational surface the
/// profile requires, at the default service standard paths, so the service works with the mesh,
/// the Spec UI, and fleet tooling out of the box:
///
/// <list type="bullet">
/// <item>the wire-envelope endpoint at <c>/benzene/invoke</c> (R4)</item>
/// <item>the derived spec at <c>/benzene/spec</c> (R5)</item>
/// <item>health checks on the reserved topic and at <c>/benzene/health</c> (R3)</item>
/// <item>the reserved <c>mesh</c> descriptor topic, trace feed, registration, and heartbeats (R6, R8)</item>
/// <item>message handlers via the registry, routed on both the HTTP and envelope pipelines (R1, R2)</item>
/// </list>
///
/// This is syntactic sugar over the same pipeline builders Benzene Core setup uses - nothing here
/// is a new capability, only the profile's steers pre-wired in the right order. Anything can be
/// overridden through <see cref="ICloudServiceBuilder"/>, and a service that wants full manual
/// control simply composes the underlying <c>Use*</c> calls itself (Benzene Core setup). Overrides
/// that step outside the profile are reflected in the <see cref="CloudServiceProfileReport"/>
/// carried on the service's descriptor, so a running service can always be asked whether it meets
/// the profile (via the reserved <c>mesh</c> topic).
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Wires the Cloud Service Profile's surfaces onto this HTTP pipeline. Add your own HTTP
    /// middleware to the pipeline <i>before</i> this call (it runs outermost); this call ends with
    /// the message router, so it is terminal - nothing should be added after it.
    /// </summary>
    /// <typeparam name="TContext">The HTTP transport's context type.</typeparam>
    /// <param name="app">The hosted HTTP pipeline to wire the service onto.</param>
    /// <param name="serviceName">The logical service name (the descriptor's required <c>service</c> field).</param>
    /// <param name="configure">Optional configuration; every setting has a profile-conformant default.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseBenzeneCloudService<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string serviceName,
        Action<ICloudServiceBuilder>? configure = null)
        where TContext : IHttpContext
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("A cloud service needs a logical service name (the descriptor's required field).", nameof(serviceName));
        }

        var builder = new CloudServiceBuilder(serviceName);
        configure?.Invoke(builder);

        var info = builder.BuildServiceInfo();
        var report = CloudServiceProfileReport.Evaluate(builder);
        builder.ProfileReportCallback?.Invoke(report);
        var descriptorSource = new CloudServiceDescriptorSource(info, report, builder.HandlerTypes);
        var healthChecks = builder.HealthChecks.ToArray();

        var announcer = builder.MeshEnabled && builder.CollectorEnvelopeUrl != null
            ? new MeshAnnouncer(info, descriptorSource, builder.CollectorEnvelopeUrl, healthChecks)
            : null;
        var traceExporter = builder.MeshEnabled
            ? builder.TraceExporter ?? (builder.CollectorEnvelopeUrl == null
                ? null
                : new HttpMeshTraceExporter(new HttpClient(), builder.CollectorEnvelopeUrl,
                    batchSize: 16, flushInterval: TimeSpan.FromSeconds(1)))
            : null;

        app.Register(x =>
        {
            x.AddBenzeneMessage();
            x.AddSingleton(_ => report);
            x.AddSingleton<IHttpEndpointDefinition>(_ =>
                new HttpEndpointDefinition("get", builder.SpecPath, Schema.OpenApi.Constants.DefaultSpecTopic));
            x.AddSingleton<IHttpEndpointDefinition>(_ =>
                new HttpEndpointDefinition("get", builder.HealthPath, HealthChecks.Constants.DefaultHealthCheckTopic));
            if (announcer != null)
            {
                // So a host whose container disposal is wired through (ASP.NET Core, the generic host)
                // stops the announce loop on shutdown instead of leaking it - see MeshAnnouncer's remarks.
                // Realized on first invocation (below) so the container actually tracks it for disposal.
                x.AddSingleton(_ => announcer);
            }
            if (traceExporter != null)
            {
                // The container owns the configured trace exporter's lifetime, so its DisposeAsync (which
                // flushes the tail trace batch) runs on shutdown. Both shipped exporters dispose
                // idempotently and implement IDisposable, so a synchronous container disposal is safe too.
                x.AddSingleton(_ => traceExporter);
            }
        });

        // A factory-registered singleton is only disposed by the container once something *resolves*
        // it (verified: MS DI never realizes a captured-instance factory that nothing resolves). The
        // announcer and exporter above are otherwise only ever used via captured locals, so without
        // this their DisposeAsync never runs on shutdown - the announce loop/HttpClient leak and the
        // exporter drops its tail trace batch. Resolve each once, on the first invocation on either
        // pipeline, so container disposal stops the loop and flushes the tail. (Hosts that never
        // dispose their provider - e.g. a short-lived Lambda container - still can't dispose them;
        // that path is the separate MicrosoftServiceResolverFactory ownership item.)
        var meshRealized = new int[1];
        void RealizeMeshDisposables<TCtx>(IMiddlewarePipelineBuilder<TCtx> pipeline)
        {
            if (announcer == null && traceExporter == null)
            {
                return;
            }

            pipeline.Use(resolver => new FuncWrapperMiddleware<TCtx>("RealizeMeshDisposables", (context, next) =>
            {
                if (Interlocked.Exchange(ref meshRealized[0], 1) == 0)
                {
                    if (announcer != null)
                    {
                        resolver.GetService<MeshAnnouncer>();
                    }
                    if (traceExporter != null)
                    {
                        resolver.GetService<IMeshTraceExporter>();
                    }
                }

                return next();
            }));
        }

        // Eager path: handler types were given, so the descriptor exists now - register with the
        // collector immediately, before any traffic. Lazy path: the registry lives in the container,
        // so the first invocation (usually a platform health probe) triggers registration instead.
        announcer?.EnsureStarted(null);

        // The wire-envelope surface (R4) and, inside it, the mesh feeds (R6/R8): trace outermost so
        // it sees every invocation, then health and descriptor interception, then the app's own
        // middleware, then the router.
        app.UseBenzeneMessage(new BenzeneMessageHttpOptions { Path = builder.InvokePath }, envelope =>
        {
            RealizeMeshDisposables(envelope);
            UseAnnouncerStart(envelope, announcer, descriptorSource);
            if (traceExporter != null)
            {
                envelope.UseMeshTrace(info, traceExporter, new BenzeneMessageMeshStatusReader());
            }
            envelope.UseHealthCheck(HealthChecks.Constants.DefaultHealthCheckTopic, healthChecks);
            if (builder.MeshEnabled)
            {
                UseDescriptor(envelope, descriptorSource);
            }
            builder.EnvelopeMiddleware?.Invoke(envelope);
            UseHandlers(envelope, builder);
        });

        // The HTTP-native surfaces: spec (R5) and health (R3) at their default-standard paths (R7),
        // and the app's own HTTP-routed topics through the same registry (R2).
        RealizeMeshDisposables(app);
        UseAnnouncerStart(app, announcer, descriptorSource);
        app.UseSpec();
        app.UseHealthCheck(HealthChecks.Constants.DefaultHealthCheckTopic, healthChecks);
        UseHandlers(app, builder);

        return app;
    }

    private static void UseHandlers<TContext>(IMiddlewarePipelineBuilder<TContext> app, CloudServiceBuilder builder)
    {
        if (builder.HandlerTypes != null)
        {
            app.UseMessageHandlers(builder.HandlerTypes);
        }
        else
        {
            app.UseMessageHandlers();
        }
    }

    /// <summary>
    /// Reserved <c>mesh</c> topic interception (mesh.md §1), like <c>UseMeshDescriptor</c> but
    /// against the shared descriptor source so the lazy path can derive the descriptor from the
    /// invocation's registry on first use.
    /// </summary>
    private static void UseDescriptor(
        IMiddlewarePipelineBuilder<BenzeneMessageContext> app, CloudServiceDescriptorSource descriptorSource)
    {
        app.Use(resolver => new FuncWrapperMiddleware<BenzeneMessageContext>("CloudServiceDescriptor", async (context, next) =>
        {
            var messageGetter = resolver.GetService<IMessageGetter<BenzeneMessageContext>>();
            var topic = messageGetter.GetTopic(context);
            if (topic?.Id != MeshTopics.Descriptor)
            {
                await next();
                return;
            }

            var resultSetter = resolver.GetService<IMessageHandlerResultSetter<BenzeneMessageContext>>();
            await resultSetter.SetResultAsync(context, new MessageHandlerResult(
                topic, MessageHandlerDefinition.Empty(), BenzeneResult.Ok(descriptorSource.Get(resolver))));
        }));
    }

    /// <summary>
    /// On the lazy path, lets the first invocation on either pipeline start registration and
    /// heartbeats. No-op middleware-wise beyond an started-already check; not wired at all when
    /// there is no announcer, and a no-op when the announcer started eagerly.
    /// </summary>
    private static void UseAnnouncerStart<TContext>(
        IMiddlewarePipelineBuilder<TContext> app, MeshAnnouncer? announcer, CloudServiceDescriptorSource descriptorSource)
    {
        if (announcer == null || descriptorSource.TryGet() != null)
        {
            return;
        }

        app.Use(resolver => new FuncWrapperMiddleware<TContext>("CloudServiceAnnounce", (context, next) =>
        {
            announcer.EnsureStarted(resolver);
            return next();
        }));
    }
}
