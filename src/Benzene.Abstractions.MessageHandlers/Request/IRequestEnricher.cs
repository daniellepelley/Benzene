namespace Benzene.Abstractions.MessageHandlers.Request;

public interface IRequestEnricher<TContext>
{
    IDictionary<string, object> Enrich<TRequest>(TRequest request, TContext context);
}