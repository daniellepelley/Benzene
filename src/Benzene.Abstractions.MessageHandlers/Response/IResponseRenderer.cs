using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// Writes a handler's result onto the transport response in some representation - JSON/XML via
/// <c>SerializerResponseRenderer{TContext}</c>, or something else entirely (HTML, a template, a raw
/// byte stream) via a custom implementation. Renderers are evaluated in registration order by
/// <c>RendererResponseHandler{TContext}</c>; the first whose <see cref="CanRender"/> returns
/// <c>true</c> wins, so a catch-all (the serializer renderer) should register last.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public interface IResponseRenderer<TContext>
{
    /// <summary>
    /// Whether this renderer should produce the response for <paramref name="result"/>, typically
    /// decided from the request's <c>accept</c> header or the result's payload type.
    /// </summary>
    /// <param name="context">The transport-specific context for the current message.</param>
    /// <param name="result">The outcome of routing and invoking the handler.</param>
    /// <param name="resolver">Resolver for any services needed to decide applicability.</param>
    bool CanRender(TContext context, IMessageHandlerResult result, IServiceResolver resolver);

    /// <summary>
    /// Writes the response body (and content type, and anything else this representation needs) onto
    /// <paramref name="response"/>. A renderer owns its own error representation - e.g. the serializer
    /// renderer keeps <c>DefaultResponsePayloadMapper</c>'s <c>ErrorPayload</c> JSON, while an HTML
    /// renderer would render its own error page.
    /// </summary>
    /// <param name="context">The transport-specific context for the current message.</param>
    /// <param name="result">The outcome of routing and invoking the handler.</param>
    /// <param name="response">Writes the body/content type/etc. onto the transport context.</param>
    Task RenderAsync(TContext context, IMessageHandlerResult result, IBenzeneResponseAdapter<TContext> response);
}
