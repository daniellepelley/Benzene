namespace Benzene.Http;

public interface IHttpRequestAdapter<TContext> where TContext : IHttpContext
{
    HttpRequest Map(TContext context);
}