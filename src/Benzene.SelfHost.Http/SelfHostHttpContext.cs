using System.Net;
using Benzene.Http;

namespace Benzene.SelfHost.Http
{
    public class SelfHostHttpContext : IHttpContext
    {
        public SelfHostHttpContext(HttpListenerContext httpListenerContext)
        {
            HttpListenerContext = httpListenerContext;
        }

        public HttpListenerContext HttpListenerContext { get; }
    }
}
