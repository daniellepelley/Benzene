using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Convenience extension methods for checking a context's topic against an expected value.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Checks whether the message extracted from <paramref name="context"/> is for the given topic.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="source">The topic getter used to extract the context's topic.</param>
    /// <param name="context">The context to check.</param>
    /// <param name="topic">The topic id to compare against.</param>
    /// <param name="isCaseSensitive">Whether the comparison is case-sensitive. Defaults to <c>false</c>.</param>
    /// <returns><c>true</c> if the context's topic matches <paramref name="topic"/>; otherwise <c>false</c> (including when the context has no topic at all).</returns>
    public static bool Is<TContext>(this IMessageTopicGetter<TContext> source, TContext context, string topic, bool isCaseSensitive = false)
    {
        var contextTopic = source.GetTopic(context);
        if (contextTopic == null)
        {
            return false;
        }

        if (!isCaseSensitive)
        {
            return contextTopic.Id.ToLowerInvariant() == topic.ToLowerInvariant();
        }

        return contextTopic.Id == topic;
    }
}

/// <summary>
/// Fluent middleware pipeline builder extensions for wiring up message-handler dispatch
/// (<see cref="MessageRouter{TContext}"/>) and registering individual handlers.
/// </summary>
public static class MiddlewarePipelineExtensions
{
    /// <summary>
    /// Adds message-handler dispatch to the pipeline, discovering handlers by reflection over every
    /// assembly currently loaded in the app domain.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder to add message-handler dispatch to.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app.UseMessageHandlers(AppDomain.CurrentDomain.GetAssemblies());
    }

    /// <summary>
    /// Adds message-handler dispatch to the pipeline, discovering handlers by reflection over every
    /// currently loaded assembly, and lets the caller register additional handler middleware/DI via
    /// <paramref name="router"/>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder to add message-handler dispatch to.</param>
    /// <param name="router">Callback used to configure the <see cref="MessageRouterBuilder"/> (e.g. add filters).</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Action<MessageRouterBuilder> router)
    {
        return app.UseMessageHandlers(AppDomain.CurrentDomain.GetAssemblies(), router);
    }

    /// <summary>
    /// Adds message-handler dispatch to the pipeline, discovering handlers by reflection over the
    /// given assemblies only.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder to add message-handler dispatch to.</param>
    /// <param name="assemblies">The assemblies to scan for handler types.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Assembly[] assemblies)
    {
        app.Register(x => x.AddMessageHandlers(assemblies));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    /// <summary>
    /// Adds message-handler dispatch to the pipeline, discovering handlers only among the given candidate types.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder to add message-handler dispatch to.</param>
    /// <param name="types">The candidate types to inspect for handler interfaces and <see cref="MessageAttribute"/>.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Type[] types)
    {
        app.Register(x => x.AddMessageHandlers(types));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    /// <summary>
    /// Adds message-handler dispatch to the pipeline, discovering handlers by reflection over a single
    /// assembly, and lets the caller register additional handler middleware/DI via <paramref name="router"/>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder to add message-handler dispatch to.</param>
    /// <param name="assembly">The assembly to scan for handler types.</param>
    /// <param name="router">Callback used to configure the <see cref="MessageRouterBuilder"/> (e.g. add filters).</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly assembly, Action<MessageRouterBuilder> router)
    {
        return app.UseMessageHandlers(new[] { assembly }, router);
    }

    /// <summary>
    /// Adds message-handler dispatch to the pipeline, discovering handlers by reflection over the
    /// given assemblies, and lets the caller register additional handler middleware/DI via <paramref name="router"/>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder to add message-handler dispatch to.</param>
    /// <param name="assemblies">The assemblies to scan for handler types.</param>
    /// <param name="router">Callback used to configure the <see cref="MessageRouterBuilder"/> (e.g. add filters).</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly[] assemblies, Action<MessageRouterBuilder> router)
    {
        return app.UseMessageHandlers(Utils.GetAllTypes(assemblies).ToArray(), router);
    }

    /// <summary>
    /// Adds message-handler dispatch to the pipeline, discovering handlers only among the given
    /// candidate types, and lets the caller register additional handler middleware/DI via
    /// <paramref name="router"/> before the pipeline is built.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder to add message-handler dispatch to.</param>
    /// <param name="types">The candidate types to inspect for handler interfaces and <see cref="MessageAttribute"/>.</param>
    /// <param name="router">Callback used to configure the <see cref="MessageRouterBuilder"/> (e.g. add filters); the builders it accumulates are added to the resolved <see cref="IHandlerPipelineBuilder"/> for every handler.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Type[] types, Action<MessageRouterBuilder> router)
    {
        app.Register(x => x.AddMessageHandlers(types));
        var builder = new MessageRouterBuilder(new List<IHandlerMiddlewareBuilder>(), app.Register);
        router(builder);

        return app.Use(resolver =>
        {
            var routePipelineBuilder = resolver.GetService<IHandlerPipelineBuilder>();
            routePipelineBuilder.Add(builder.GetBuilders());
            return resolver.GetService<MessageRouter<TContext>>();
        });
    }

    /// <summary>
    /// Sets a fixed topic on every message that flows through this pipeline, via
    /// <see cref="PresetTopicMiddleware{TContext}"/>, so <c>UseMessageHandlers</c> routes on it
    /// regardless of what (if anything) the underlying transport message itself carries. Intended
    /// for a queue/subscription whose producer doesn't set Benzene's usual topic attribute/property -
    /// call this before <c>UseMessageHandlers</c> in that specific pipeline only; a queue that does
    /// send a proper topic just omits it.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type, which must be able to carry a preset topic.</typeparam>
    /// <param name="app">The pipeline builder to add the preset topic to.</param>
    /// <param name="topicId">The topic id every message on this pipeline should route to.</param>
    /// <param name="version">The optional topic version. Defaults to an empty string (unversioned).</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UsePresetTopic<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string topicId, string version = "")
        where TContext : IHasPresetTopic
    {
        var presetTopic = new Topic(topicId, version);
        return app.Use(_ => new PresetTopicMiddleware<TContext>(presetTopic));
    }

    /// <summary>
    /// Registers a request/response handler explicitly, without relying on reflection-based discovery.
    /// </summary>
    /// <typeparam name="THandler">The concrete handler type, registered as scoped (if not already registered).</typeparam>
    /// <typeparam name="TRequest">The handler's strongly-typed request type.</typeparam>
    /// <typeparam name="TResponse">The handler's strongly-typed response type.</typeparam>
    /// <param name="builder">The router builder to register the handler with.</param>
    /// <param name="topic">The topic id this handler answers.</param>
    /// <param name="version">The optional version of this handler. Defaults to an empty string (unversioned).</param>
    public static void AddMessageHandler<THandler, TRequest, TResponse>(this IMessageRouterBuilder builder, string topic, string? version = null)
        where THandler : class, IMessageHandler<TRequest, TResponse>
        where TRequest : class
    {
        builder.Register(x =>
        {
            x.TryAddScoped<THandler>();
            x.AddSingleton<IMessageHandlerDefinition>(MessageHandlerDefinition.CreateInstance(topic, version ?? string.Empty, typeof(TRequest), typeof(TResponse), typeof(THandler)));
        });
    }

    /// <summary>
    /// Registers a no-response (fire-and-forget) handler explicitly, without relying on reflection-based discovery.
    /// </summary>
    /// <typeparam name="THandler">The concrete handler type, registered as scoped (if not already registered).</typeparam>
    /// <typeparam name="TRequest">The handler's strongly-typed request type.</typeparam>
    /// <param name="builder">The router builder to register the handler with.</param>
    /// <param name="topic">The topic id this handler answers.</param>
    /// <param name="version">The optional version of this handler. Defaults to an empty string (unversioned).</param>
    public static void AddMessageHandler<THandler, TRequest>(this IMessageRouterBuilder builder, string topic, string? version = null)
        where THandler : class, IMessageHandler<TRequest>
        where TRequest : class
    {
        builder.Register(x =>
        {
            x.TryAddScoped<THandler>();
            x.AddSingleton<IMessageHandlerDefinition>(MessageHandlerDefinition.CreateInstance(topic, version ?? string.Empty, typeof(TRequest), typeof(Benzene.Abstractions.Results.Void), typeof(THandler)));
        });
    }
}
