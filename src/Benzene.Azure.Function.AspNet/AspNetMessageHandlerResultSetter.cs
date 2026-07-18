using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Sets the message handler result on an <see cref="AspNetContext"/> by running the configured response
/// handlers (e.g. body, status code) against it.
/// </summary>
public class AspNetMessageHandlerResultSetter : ResponseMessageHandlerResultSetterBase<AspNetContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetMessageHandlerResultSetter"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">The container of response handlers to run against the context.</param>
    public AspNetMessageHandlerResultSetter(IResponseHandlerContainer<AspNetContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}
