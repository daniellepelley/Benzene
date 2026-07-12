using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.AspNet;

/// <summary>
/// Sets the message handler result on an <see cref="AspNetContext"/> by running the configured response
/// handlers (e.g. body, status code) against it.
/// </summary>
public class AspNetMessageMessageHandlerResultSetter : ResponseMessageMessageHandlerResultSetterBase<AspNetContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetMessageMessageHandlerResultSetter"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">The container of response handlers to run against the context.</param>
    public AspNetMessageMessageHandlerResultSetter(IResponseHandlerContainer<AspNetContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}
