namespace Benzene.Abstractions.Response;

public interface IBenzeneResponseAdapter<TContext>
{
    void SetResponseHeader(TContext context, string headerKey, string headerValue);
    void SetContentType(TContext context, string contentType);
    void SetStatusCode(TContext context, string statusCode);
    void SetBody(TContext context, string body);
    string GetBody(TContext context);
    Task FinalizeAsync(TContext context);
}