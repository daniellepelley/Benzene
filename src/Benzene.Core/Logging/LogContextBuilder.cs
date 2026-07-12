using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

/// <summary>
/// Builds log scope state for request and response phases of processing.
/// </summary>
/// <typeparam name="TContext">The type of context to build log scopes from.</typeparam>
public class LogContextBuilder<TContext> : ILogContextBuilder<TContext>
{
    private readonly IContextDictionaryBuilder<TContext> _requestContextDictionaryBuilder = new ContextDictionaryBuilder<TContext>();
    private readonly IContextDictionaryBuilder<TContext> _responseContextDictionaryBuilder = new ContextDictionaryBuilder<TContext>();
    private readonly IRegisterDependency _registerDependency;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogContextBuilder{TContext}"/> class.
    /// </summary>
    /// <param name="registerDependency">The dependency registration interface.</param>
    public LogContextBuilder(IRegisterDependency registerDependency)
    {
        _registerDependency = registerDependency;
    }

    /// <summary>
    /// Configures log scope properties to be applied during the request phase.
    /// </summary>
    /// <param name="dictionaryAction">The function to extract log properties from the request.</param>
    /// <returns>The builder for method chaining.</returns>
    public ILogContextBuilder<TContext> OnRequest(Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction)
    {
        _requestContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }

    /// <summary>
    /// Configures log scope properties to be applied during the response phase.
    /// </summary>
    /// <param name="dictionaryAction">The function to extract log properties from the response.</param>
    /// <returns>The builder for method chaining.</returns>
    public ILogContextBuilder<TContext> OnResponse(Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction)
    {
        _responseContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }

    /// <summary>
    /// Builds the request-phase scope state from the configured request properties.
    /// </summary>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The scope state as a key/value dictionary.</returns>
    public IReadOnlyDictionary<string, object> BuildRequestScope(IServiceResolver serviceResolver, TContext context)
    {
        return Build(_requestContextDictionaryBuilder, serviceResolver, context);
    }

    /// <summary>
    /// Builds the response-phase scope state from the configured response properties.
    /// </summary>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <param name="context">The response context.</param>
    /// <returns>The scope state as a key/value dictionary.</returns>
    public IReadOnlyDictionary<string, object> BuildResponseScope(IServiceResolver serviceResolver, TContext context)
    {
        return Build(_responseContextDictionaryBuilder, serviceResolver, context);
    }

    /// <summary>
    /// Registers dependencies into the service container.
    /// </summary>
    /// <param name="action">The action that performs the registration.</param>
    public void Register(Action<IBenzeneServiceContainer> action)
    {
        _registerDependency.Register(action);
    }

    private static IReadOnlyDictionary<string, object> Build(IContextDictionaryBuilder<TContext> builder,
        IServiceResolver serviceResolver, TContext context)
    {
        return builder.Build(serviceResolver, context)
            .ToDictionary(x => x.Key, x => (object)x.Value);
    }
}
