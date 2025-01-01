// using Benzene.Abstractions.MessageHandlers;
// using Benzene.Results;
//
// namespace Benzene.Abstractions.MessageHandling;
//
// public interface IMessageSender<TRequest>
// {
//     Task SendMessageAsync(TRequest request);
// }
//
// public interface IMessageSender<TRequest, TResponse> : IMessageHandlerBase<TRequest, TResponse>
// {
//     Task<IBenzeneResult<TResponse>> SendMessageAsync(TRequest request);
// }
