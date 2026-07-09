using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class ExceptionHandlerMiddleware<TContext>(Action<TContext, Exception> onException) : IMiddleware<TContext>
{
    public string Name => "ExceptionHandler";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            onException(context, ex);
        }
    }
}
