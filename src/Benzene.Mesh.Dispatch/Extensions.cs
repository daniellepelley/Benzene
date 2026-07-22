using System.Net.Http;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;

namespace Benzene.Mesh.Dispatch;

/// <summary>Opt-in wiring for the mesh dispatch feature.</summary>
public static class Extensions
{
    /// <summary>The reserved-style topic the dispatch handler is served on.</summary>
    public const string DispatchTopic = "mesh:dispatch";

    /// <summary>
    /// Registers the opt-in <c>mesh:dispatch</c> handler, which invokes ONE registered service's real
    /// handler with a caller-supplied payload (see <see cref="MeshDispatchMessageHandler"/>). Opt-in by
    /// construction (nothing is exposed unless this is called) AND gated at runtime: dispatch is refused
    /// in a Production environment unless <see cref="MeshDispatchOptions.AllowInProduction"/> is set,
    /// because a real handler runs with real side-effects.
    /// </summary>
    /// <remarks>
    /// Requires a <c>Benzene.Mesh.Contracts.MeshServiceRegistry</c> registered in the same container
    /// (the set of services that can be targeted) and at least one <see cref="IMeshServiceDispatcher"/>
    /// for the transports in use (the HTTP dispatcher is registered here; add
    /// <c>Benzene.Mesh.Aws.Lambda.Extensions.AddMeshLambdaDispatcher()</c> for AWS-Lambda services).
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshDispatch<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, MeshDispatchOptions? options = null)
    {
        app.Register(x =>
        {
            x.AddSingleton(options ?? new MeshDispatchOptions());
            x.TryAddSingleton<IMeshDispatchEnvironment>(_ => new EnvironmentVariableMeshDispatchEnvironment());
            x.AddSingleton<MeshDispatchGate>();
            x.TryAddSingleton(_ => new HttpClient());
            x.AddSingleton<IMeshServiceDispatcher>(resolver => new HttpMeshServiceDispatcher(resolver.GetService<HttpClient>()));
            x.AddSingleton<IMessageHandlerDefinition>(_ =>
                MessageHandlerDefinition.CreateInstance(DispatchTopic, "", typeof(MeshDispatchRequest),
                    typeof(RawStringMessage),
                    typeof(MeshDispatchMessageHandler)));
            x.AddScoped<MeshDispatchMessageHandler>();
        });
        return app;
    }
}
