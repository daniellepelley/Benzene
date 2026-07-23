using System.Net;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Http;

namespace Benzene.SelfHost.Http;

public class SelfHostHttpContext : IHttpContext, IHasMessageResult
{
    public SelfHostHttpContext(HttpListenerContext httpListenerContext)
    {
        HttpListenerContext = httpListenerContext;
    }

    public HttpListenerContext HttpListenerContext { get; }

    /// <summary>
    /// The outcome of handling this request, recorded by <see cref="HttpListenerMessageHandlerResultSetter"/>
    /// so a cross-cutting observer of the completed pipeline (e.g. metrics) sees a real success/failure signal.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; } = null!;
}
