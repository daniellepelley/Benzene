using Benzene.Results;

namespace Benzene.Clients;

// public interface IBenzeneClientRequest<TMessage>
// {
//     public string Topic { get; }
//     public TMessage Message { get; }
//     public IDictionary<string, string> Headers { get; }
// }

// public interface IBenzeneClientContext<TRequest, TResponse>
// {
//     IBenzeneClientRequest<TRequest> Request { get; }
//     IBenzeneResult<TResponse> Response { get; set; }
// }

// public class BenzeneClientContext<TRequest, TResponse> : IBenzeneClientContext<TRequest, TResponse>
// {
//     public BenzeneClientContext(IBenzeneClientRequest<TRequest> request)
//     {
//         Request = request;
//     }
//     
//     public IBenzeneClientRequest<TRequest> Request { get; }
//     public IBenzeneResult<TResponse> Response { get; set; }
// }