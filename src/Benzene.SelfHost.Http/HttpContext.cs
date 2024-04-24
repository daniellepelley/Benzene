using System.Net;
using Benzene.Abstractions.Results;

namespace Benzene.SelfHost.Http
{
    public class HttpContext : IHasMessageResult
    {
        public HttpContext(HttpListenerContext httpListenerContext)
        {
            HttpListenerContext = httpListenerContext;
            MessageResult = Core.Results.MessageResult.Empty();
        }

        public HttpListenerContext HttpListenerContext { get; }
        public IMessageResult MessageResult { get; set; }
    }
}
