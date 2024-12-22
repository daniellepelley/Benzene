using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Core.Response;

// public class ResponseIfHandledMiddleware<TContext> : IMiddleware<TContext> where TContext : class
// {
//     private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;
//
//     public ResponseIfHandledMiddleware(IResponseHandlerContainer<TContext> responseHandlerContainer)
//     {
//         _responseHandlerContainer = responseHandlerContainer;
//     }
//
//     public string Name => "Response If Handled";
//
//     public async Task HandleAsync(TContext context, Func<Task> next)
//     {
//         await next();
//
//         var topic = context.MessageResult.MessageHandlerDefinition?.Topic;
//         if (topic != null && topic.Id != Constants.Missing)
//         {
//             await _responseHandlerContainer.HandleAsync(context);
//         }
//     }
// }

public class ResponseIfHandledResultSetter<TContext> : IResultSetter<TContext> where TContext : class
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    public ResponseIfHandledResultSetter(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    public async Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        if (messageHandlerResult.Topic != null && messageHandlerResult.Topic.Id != Constants.Missing)
        {
            await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
        }
    }
}

