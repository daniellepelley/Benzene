using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Extras.Response;

public interface IResponseBuilder<TContext> where TContext : class
{
    IResponseBuilder<TContext> Add<T>() where T: class, IResponseHandler<TContext>;
    IResponseBuilder<TContext> Add(Func<IServiceResolver, IResponseHandler<TContext>> func);
    Func<IServiceResolver, IResponseHandler<TContext>>[] GetBuilders();
}
