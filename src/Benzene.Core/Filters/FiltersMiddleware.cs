using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Core.Filters;

public class FiltersMiddleware<TRequest, TResponse> : IMiddleware<IMessageContext<TRequest, TResponse>> 
    where TRequest : class
{
    private readonly IServiceResolver _serviceResolver;

    public FiltersMiddleware(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    public string Name => "Filters";

    public async Task HandleAsync(IMessageContext<TRequest, TResponse> context, Func<Task> next)
    {
        var filter = _serviceResolver.TryGetService<IFilter<TRequest>>();
        if (filter != null)
        {
            var canProcess = filter.Filter(context.Request);
            if (!canProcess)
            {
                context.Response =
                    ServiceResult.Ignored<TResponse>();
                return;
            }
        }
        await next();
    }
}
