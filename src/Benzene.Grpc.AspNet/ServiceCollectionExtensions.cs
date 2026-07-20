using Benzene.Grpc;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using IBenzeneHealthCheck = Benzene.HealthChecks.Core.IHealthCheck;

namespace Benzene.Grpc.AspNet;

/// <summary>
/// Provides extension methods for registering gRPC services and the Benzene interceptor in ASP.NET Core.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ASP.NET Core's gRPC services and adds <see cref="BenzeneInterceptor"/> so that gRPC calls
    /// matching a <see cref="GrpcMethodAttribute"/>-decorated message handler are routed through Benzene's
    /// message handler pipeline.
    /// </summary>
    /// <param name="services">The service collection to register gRPC services with.</param>
    /// <param name="configure">Optional additional gRPC service configuration.</param>
    /// <returns><paramref name="services"/>, for method chaining.</returns>
    /// <remarks>
    /// Registers a single shared <see cref="IGrpcMethodHandlerFactoryAccessor"/> instance. <c>UseGrpc</c>
    /// populates its <see cref="IGrpcMethodHandlerFactoryAccessor.Factory"/> once the middleware pipeline
    /// is built; because the registration is instance-based it resolves to the same object from both the
    /// application's root service provider (which activates <see cref="BenzeneInterceptor"/> per call) and
    /// Benzene's own pipeline-building container.
    /// </remarks>
    public static IServiceCollection AddBenzeneGrpc(this IServiceCollection services, Action<GrpcServiceOptions>? configure = null)
    {
        return services.AddBenzeneGrpc(options => options.ConfigureGrpc = configure);
    }

    /// <summary>
    /// Registers ASP.NET Core's gRPC services and adds <see cref="BenzeneInterceptor"/>, same as the
    /// other overload, plus the optional health check
    /// (<see cref="BenzeneGrpcOptions.EnableHealthChecks"/>) and reflection
    /// (<see cref="BenzeneGrpcOptions.EnableReflection"/>) services - both off by default. Map the
    /// resulting endpoints with <see cref="GrpcEndpointExtensions"/>.
    /// </summary>
    /// <param name="services">The service collection to register gRPC services with.</param>
    /// <param name="configureOptions">Configures <see cref="BenzeneGrpcOptions"/>.</param>
    /// <returns><paramref name="services"/>, for method chaining.</returns>
    public static IServiceCollection AddBenzeneGrpc(this IServiceCollection services, Action<BenzeneGrpcOptions> configureOptions)
    {
        var options = new BenzeneGrpcOptions();
        configureOptions(options);

        services.TryAdd(ServiceDescriptor.Singleton<IGrpcMethodHandlerFactoryAccessor>(new GrpcMethodHandlerFactoryAccessor()));

        services.AddGrpc(grpcOptions =>
        {
            grpcOptions.Interceptors.Add(typeof(BenzeneInterceptor));
            options.ConfigureGrpc?.Invoke(grpcOptions);
        });

        if (options.EnableHealthChecks)
        {
            var hasSplit = options.LivenessCheckTypes != null || options.ReadinessCheckTypes != null;

            var grpcHealthChecks = services.AddGrpcHealthChecks(o =>
            {
                if (!hasSplit)
                {
                    return; // Default: all checks map to the overall "" service, exactly as before.
                }

                // Overall "" service stays the aggregate check; named services map by tag.
                o.Services.Map("", ctx => ctx.Name == "benzene");
                if (options.LivenessCheckTypes != null)
                {
                    o.Services.Map("liveness", ctx => ctx.Tags.Contains("liveness"));
                }
                if (options.ReadinessCheckTypes != null)
                {
                    o.Services.Map("readiness", ctx => ctx.Tags.Contains("readiness"));
                }
            });

            grpcHealthChecks.AddCheck<BenzeneHealthCheckBridge>("benzene");

            if (options.LivenessCheckTypes != null)
            {
                var livenessTypes = new HashSet<string>(options.LivenessCheckTypes);
                grpcHealthChecks.Add(new HealthCheckRegistration("benzene-liveness",
                    sp => new BenzeneHealthCheckBridge(sp.GetServices<IBenzeneHealthCheck>(), livenessTypes),
                    failureStatus: null, tags: new[] { "liveness" }));
            }
            if (options.ReadinessCheckTypes != null)
            {
                var readinessTypes = new HashSet<string>(options.ReadinessCheckTypes);
                grpcHealthChecks.Add(new HealthCheckRegistration("benzene-readiness",
                    sp => new BenzeneHealthCheckBridge(sp.GetServices<IBenzeneHealthCheck>(), readinessTypes),
                    failureStatus: null, tags: new[] { "readiness" }));
            }
        }

        if (options.EnableReflection)
        {
            services.AddGrpcReflection();
        }

        return services;
    }
}
