using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class InlineContextConverter<TContextIn, TContextOut>(
    Func<TContextIn, TContextOut> createContextFunc,
    Action<TContextIn, TContextOut> mapContext)
    : IContextConverter<TContextIn, TContextOut>
{
    public Task<TContextOut> CreateRequestAsync(TContextIn contextIn)
        => Task.FromResult(createContextFunc(contextIn));

    public Task MapResponseAsync(TContextIn contextIn, TContextOut contextOut)
    {
        mapContext(contextIn, contextOut);
        return Task.CompletedTask;
    }
}