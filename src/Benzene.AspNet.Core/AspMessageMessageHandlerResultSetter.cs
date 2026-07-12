using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Response;

namespace Benzene.AspNet.Core;

/// <summary>
/// Sets the message handler result on an <see cref="AspNetContext"/> by running the configured response
/// handlers only if the request was actually matched and handled — leaving the underlying ASP.NET Core
/// pipeline untouched (so <c>next()</c> can continue to other middleware/endpoints) for requests that
/// didn't match a message handler.
/// </summary>
public class AspMessageMessageHandlerResultSetter : ResponseIfHandledMessageHandlerResultSetter<AspNetContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspMessageMessageHandlerResultSetter"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">The container of response handlers to run against the context.</param>
    public AspMessageMessageHandlerResultSetter(IResponseHandlerContainer<AspNetContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}
