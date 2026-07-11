using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;

namespace Benzene.Core.Logging;

/// <summary>
/// Builds log context configuration for request and response phases of processing.
/// </summary>
/// <typeparam name="TContext">The type of context to build log context from.</typeparam>
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
    /// Configures log context to be applied during the request phase.
    /// </summary>
    /// <param name="dictionaryAction">The function to extract log context from the request.</param>
    /// <returns>The builder for method chaining.</returns>
    public ILogContextBuilder<TContext> OnRequest(Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction)
    {
        _requestContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }

    /// <summary>
    /// Configures log context to be applied during the response phase.
    /// </summary>
    /// <param name="dictionaryAction">The function to extract log context from the response.</param>
    /// <returns>The builder for method chaining.</returns>
    public ILogContextBuilder<TContext> OnResponse(Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction)
    {
        _responseContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }

    /// <summary>
    /// Creates a disposable log context scope for the request phase.
    /// </summary>
    /// <param name="benzeneLogContext">The log context to create the scope in.</param>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <param name="context">The request context.</param>
    /// <returns>A disposable scope that removes the log context when disposed.</returns>
    public IDisposable CreateForRequest(IBenzeneLogContext benzeneLogContext, IServiceResolver serviceResolver, TContext context)
    {
        return benzeneLogContext.Create(_requestContextDictionaryBuilder.Build(serviceResolver, context));
    }

    /// <summary>
    /// Creates a disposable log context scope for the response phase.
    /// </summary>
    /// <param name="benzeneLogContext">The log context to create the scope in.</param>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <param name="context">The response context.</param>
    /// <returns>A disposable scope that removes the log context when disposed.</returns>
    public IDisposable CreateForResponse(IBenzeneLogContext benzeneLogContext, IServiceResolver serviceResolver, TContext context)
    {
        return benzeneLogContext.Create(_responseContextDictionaryBuilder.Build(serviceResolver, context));
    }

    /// <summary>
    /// Registers dependencies into the service container.
    /// </summary>
    /// <param name="action">The action that performs the registration.</param>
    public void Register(Action<IBenzeneServiceContainer> action)
    {
        _registerDependency.Register(action);
    }
}
