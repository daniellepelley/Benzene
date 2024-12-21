using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Response;

public class ResponseIfHandledMiddleware<TContext> : IMiddleware<TContext> where TContext : class, IHasMessageResult
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    public ResponseIfHandledMiddleware(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    public string Name => "Response If Handled";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        await next();

        var topic = context.MessageResult.MessageHandlerDefinition?.Topic;
        if (topic != null && topic.Id != Constants.Missing)
        {
            await _responseHandlerContainer.HandleAsync(context);
        }
    }
}
