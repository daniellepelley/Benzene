using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class InlineContextConverter<TContextIn, TContextOut> : IContextConverter<TContextIn, TContextOut>
{
    private readonly Func<TContextIn, TContextOut> _createContextFunc;
    private readonly Action<TContextIn, TContextOut> _mapContext;

    public InlineContextConverter(Func<TContextIn, TContextOut> createContextFunc, Action<TContextIn, TContextOut> mapContext)
    {
        _mapContext = mapContext;
        _createContextFunc = createContextFunc;
    }

    public Task<TContextOut> CreateRequestAsync(TContextIn contextIn)
        => Task.FromResult(_createContextFunc(contextIn));

    public Task MapResponseAsync(TContextIn contextIn, TContextOut contextOut)
    {
        _mapContext(contextIn, contextOut);
        return Task.CompletedTask;
    }
}