// using System;
// using System.Threading.Tasks;
// using Benzene.Abstractions.Middleware;
// using Benzene.Abstractions.Response;
// using Benzene.Abstractions.Results;
//
// namespace Benzene.Core.Response;
//
// public class ResponseMiddleware<TContext> : IMiddleware<TContext> where TContext : class, IHasMessageResult
// {
//     private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;
//
//     public ResponseMiddleware(IResponseHandlerContainer<TContext> responseHandlerContainer)
//     {
//         _responseHandlerContainer = responseHandlerContainer;
//     }
//
//     public string Name => "Response";
//
//     public async Task HandleAsync(TContext context, Func<Task> next)
//     {
//         await next();
//         await _responseHandlerContainer.HandleAsync(context);
//     }
// }