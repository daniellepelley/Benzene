using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// The single <see cref="IResponseHandler{TContext}"/> every transport registers to write response
/// bodies: short-circuits if a body has already been set, otherwise walks the registered
/// <see cref="IResponseRenderer{TContext}"/>s in order and delegates to the first whose
/// <see cref="IResponseRenderer{TContext}.CanRender"/> matches.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class RendererResponseHandler<TContext> : IResponseHandler<TContext> where TContext : class
{
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;
    private readonly IResponseRenderer<TContext>[] _renderers;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="RendererResponseHandler{TContext}"/> class.
    /// </summary>
    /// <param name="benzeneResponseAdapter">Reads/writes the body on the transport context.</param>
    /// <param name="renderers">Every registered renderer to consider, in registration order.</param>
    /// <param name="serviceResolver">Resolver passed to each renderer's applicability check.</param>
    public RendererResponseHandler(
        IBenzeneResponseAdapter<TContext> benzeneResponseAdapter,
        IEnumerable<IResponseRenderer<TContext>> renderers,
        IServiceResolver serviceResolver)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _renderers = renderers as IResponseRenderer<TContext>[] ?? renderers.ToArray();
        _serviceResolver = serviceResolver;
    }

    /// <inheritdoc />
    public async ValueTask HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        if (!string.IsNullOrEmpty(_benzeneResponseAdapter.GetBody(context)))
        {
            return;
        }

        var renderer = _renderers.FirstOrDefault(r => r.CanRender(context, messageHandlerResult, _serviceResolver));
        if (renderer == null)
        {
            return;
        }

        await renderer.RenderAsync(context, messageHandlerResult, _benzeneResponseAdapter);
    }
}
