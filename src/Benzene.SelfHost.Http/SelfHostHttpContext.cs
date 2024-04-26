using System.Net;
using Benzene.Abstractions.Results;
using Benzene.Http;

namespace Benzene.SelfHost.Http
{
    public class SelfHostHttpContext : IHasMessageResult, IHttpContext
    {
        public SelfHostHttpContext(HttpListenerContext httpListenerContext)
        {
            HttpListenerContext = httpListenerContext;
            MessageResult = Core.Results.MessageResult.Empty();
        }

        public HttpListenerContext HttpListenerContext { get; }

        public IMessageResult MessageResult { get; set; }
    }
}
