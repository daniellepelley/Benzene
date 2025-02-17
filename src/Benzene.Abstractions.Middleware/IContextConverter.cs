namespace Benzene.Abstractions.Middleware;

public interface IContextConverter<TContextIn, TContextOut>
{
    public Task<TContextOut> CreateRequestAsync(TContextIn contextIn);
    public Task MapResponseAsync(TContextIn contextIn, TContextOut contextOut);
}