using Benzene.Abstractions.Results;

namespace Benzene.SelfHost.Http
{
    public class HttpContext : IHasMessageResult
    {
        public HttpContext(HttpRequest httpRequest)
        {
            Request = httpRequest;
            MessageResult = Core.Results.MessageResult.Empty();
        }

        public HttpRequest Request { get; }
        public HttpResponse Response { get; set; }
        public IMessageResult MessageResult { get; set; }
    }
}
