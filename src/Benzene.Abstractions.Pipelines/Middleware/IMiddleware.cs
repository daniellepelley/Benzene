namespace Benzene.Abstractions.Middleware;

public interface IMiddleware<in TContext>
{
    string Name { get; }
    Task HandleAsync(TContext context, Func<Task> next);
}
