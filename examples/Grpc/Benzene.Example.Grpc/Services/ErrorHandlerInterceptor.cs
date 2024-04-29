// using Grpc.Core;
// using Grpc.Core.Interceptors;
//
// namespace Benzene.Example.Grpc.Services;
//
// public interface IBenzeneGrpc
// {
//     
// }
//
// public class BenzeneGrpc : IBenzeneGrpc
// {
//     
// }
//
// public class ErrorHandlerInterceptor : Interceptor
// {
//     private IBenzeneGrpc _benzeneGrpc;
//
//     public ErrorHandlerInterceptor(IBenzeneGrpc benzeneGrpc)
//     {
//         _benzeneGrpc = benzeneGrpc;
//     }
//     
//     public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
//         TRequest request,
//         ClientInterceptorContext<TRequest, TResponse> context,
//         AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
//     {
//         var call = continuation(request, context);
//
//         return new AsyncUnaryCall<TResponse>(
//             HandleResponse(call.ResponseAsync),
//             call.ResponseHeadersAsync,
//             call.GetStatus,
//             call.GetTrailers,
//             call.Dispose);
//     }
//
//     public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
//         UnaryServerMethod<TRequest, TResponse> continuation)
//     {
//         return base.UnaryServerHandler(request, context, continuation);
//     }
//
//     private async Task<TResponse> HandleResponse<TResponse>(Task<TResponse> inner)
//     {
//         try
//         {
//             return await inner;
//         }
//         catch (Exception ex)
//         {
//             throw new InvalidOperationException("Custom error", ex);
//         }
//     }
// }