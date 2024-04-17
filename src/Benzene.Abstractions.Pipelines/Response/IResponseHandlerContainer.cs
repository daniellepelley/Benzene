using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.Response;

/// <summary>
/// Handles creation of a response
/// </summary>
public interface IResponseHandlerContainer<TContext> where TContext : class, IHasMessageResult
{
    Task HandleAsync(TContext context);
}