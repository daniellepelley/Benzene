// using System;
// using System.Threading.Tasks;
// using Benzene.Abstractions.Results;
//
// namespace Benzene.Core.Results;
//
// public static class ClientResultExtensions
// {
//     public static IServiceResult<T> AsServiceResult<T>(this IClientResult<T> clientResult)
//     {
//         return AsServiceResult(clientResult, clientResult.Payload);
//     }
//
//     public static IServiceResult<TOutput> AsServiceResult<TInput, TOutput>(this IClientResult<TInput> clientResult, TOutput payload)
//     {
//         switch (clientResult.Status)
//         {
//             case ClientResultStatus.Accepted:
//                 return ServiceResult.Accepted(payload);
//             case ClientResultStatus.Success:
//                 return ServiceResult.Ok(payload);
//             case ClientResultStatus.Created:
//                 return ServiceResult.Created(payload);
//             case ClientResultStatus.Updated:
//                 return ServiceResult.Updated(payload);
//             case ClientResultStatus.Deleted:
//                 return ServiceResult.Deleted(payload);
//             case ClientResultStatus.NotFound:
//                 return ServiceResult.NotFound<TOutput>(clientResult.Errors);
//             case ClientResultStatus.Conflict:
//                 return ServiceResult.Conflict<TOutput>(clientResult.Errors);
//             case ClientResultStatus.ServiceUnavailable:
//             case ClientResultStatus.UnexpectedError:
//                 return ServiceResult.ServiceUnavailable<TOutput>(clientResult.Errors);
//             default:
//                 return ServiceResult.ServiceUnavailable<TOutput>();
//         }
//     }
//
//     public static IServiceResult<T> AsServiceResult<T>(this IClientResult clientResult)
//     {
//         switch (clientResult.Status)
//         {
//             case ClientResultStatus.Accepted:
//                 return ServiceResult.Accepted<T>();
//             case ClientResultStatus.Success:
//                 return ServiceResult.Ok<T>();
//             case ClientResultStatus.Created:
//                 return ServiceResult.Created<T>();
//             case ClientResultStatus.Updated:
//                 return ServiceResult.Updated<T>();
//             case ClientResultStatus.Deleted:
//                 return ServiceResult.Deleted<T>();
//             case ClientResultStatus.NotFound:
//                 return ServiceResult.NotFound<T>(clientResult.Errors);
//             case ClientResultStatus.Conflict:
//                 return ServiceResult.Conflict<T>(clientResult.Errors);
//             case ClientResultStatus.ServiceUnavailable:
//             case ClientResultStatus.UnexpectedError:
//                 return ServiceResult.ServiceUnavailable<T>(clientResult.Errors);
//             default:
//                 return ServiceResult.ServiceUnavailable<T>();
//         }
//     }
//
//     public static IServiceResult AsServiceResult(this IClientResult clientResult)
//     {
//         switch (clientResult.Status)
//         {
//             case ClientResultStatus.Accepted:
//                 return ServiceResult.Accepted(clientResult.PayloadAsObject);
//             case ClientResultStatus.Success:
//                 return ServiceResult.Ok(clientResult.PayloadAsObject);
//             case ClientResultStatus.Created:
//                 return ServiceResult.Created(clientResult.PayloadAsObject);
//             case ClientResultStatus.Updated:
//                 return ServiceResult.Updated(clientResult.PayloadAsObject);
//             case ClientResultStatus.Deleted:
//                 return ServiceResult.Deleted();
//             case ClientResultStatus.NotFound:
//                 return ServiceResult.NotFound(clientResult.Errors);
//             case ClientResultStatus.Conflict:
//                 return ServiceResult.Conflict(clientResult.Errors);
//             case ClientResultStatus.ServiceUnavailable:
//             case ClientResultStatus.UnexpectedError:
//                 return ServiceResult.ServiceUnavailable(clientResult.Errors);
//             default:
//                 return ServiceResult.ServiceUnavailable();
//         }
//     }
//
//     public static Task<IServiceResult<T>> AsServiceResult<T>(this Task<IClientResult<T>> source)
//     {
//         return source.ContinueWith(clientResult => clientResult.Result.AsServiceResult());
//     }
//
//     public static async Task<IServiceResult<TOutput>> AsServiceResultMapIfSuccessful<TInput, TOutput>(this Task<IClientResult<TInput>> source, Func<TInput, TOutput> map)
//     {
//         await source;
//         if (source.Result.IsSuccessful)
//         {
//             return source.Result.AsServiceResult(map(source.Result.Payload));
//
//         }
//
//         return source.Result.AsServiceResult<TOutput>();
//     }
//
//     // public static bool IsSuccessful<T>(this IClientResult<T> source)
//     // {
//     //     return new[]
//     //     {
//     //         ClientResultStatus.Success,
//     //         ClientResultStatus.Created,
//     //         ClientResultStatus.Updated,
//     //         ClientResultStatus.Deleted
//     //     }.Contains(source.Status);
//     // }
//
//     public static async Task<IClientResult<TOutput>> MapIfSuccessful<TInput, TOutput>(this Task<IClientResult<TInput>> source, Func<TInput, TOutput> map)
//     {
//         await source;
//         if (source.Result.IsSuccessful)
//         {
//             return source.Result.AsType(map(source.Result.Payload));
//
//         }
//
//         return source.Result.AsType<TOutput>();
//     }
//
//     public static IClientResult<TOutput> AsType<TInput, TOutput>(this IClientResult<TInput> clientResult, TOutput payload)
//     {
//         switch (clientResult.Status)
//         {
//             case ClientResultStatus.Accepted:
//                 return ClientResult.Accepted(payload);
//             case ClientResultStatus.Success:
//                 return ClientResult.Ok(payload);
//             case ClientResultStatus.Created:
//                 return ClientResult.Created(payload);
//             case ClientResultStatus.Updated:
//                 return ClientResult.Updated(payload);
//             case ClientResultStatus.Deleted:
//                 return ClientResult.Deleted(payload);
//             case ClientResultStatus.NotFound:
//                 return ClientResult.NotFound<TOutput>(clientResult.Errors);
//             case ClientResultStatus.Conflict:
//                 return ClientResult.Conflict<TOutput>(clientResult.Errors);
//             case ClientResultStatus.ServiceUnavailable:
//             case ClientResultStatus.UnexpectedError:
//                 return ClientResult.ServiceUnavailable<TOutput>(clientResult.Errors);
//             default:
//                 return ClientResult.ServiceUnavailable<TOutput>();
//         }
//     }
//
//     public static IClientResult<T> AsType<T>(this IClientResult clientResult)
//     {
//         switch (clientResult.Status)
//         {
//             case ClientResultStatus.Accepted:
//                 return ClientResult.Accepted<T>();
//             case ClientResultStatus.Success:
//                 return ClientResult.Ok<T>();
//             case ClientResultStatus.Created:
//                 return ClientResult.Created<T>();
//             case ClientResultStatus.Updated:
//                 return ClientResult.Updated<T>();
//             case ClientResultStatus.Deleted:
//                 return ClientResult.Deleted<T>();
//             case ClientResultStatus.NotFound:
//                 return ClientResult.NotFound<T>(clientResult.Errors);
//             case ClientResultStatus.Conflict:
//                 return ClientResult.Conflict<T>(clientResult.Errors);
//             case ClientResultStatus.ServiceUnavailable:
//             case ClientResultStatus.UnexpectedError:
//                 return ClientResult.ServiceUnavailable<T>(clientResult.Errors);
//             default:
//                 return ClientResult.ServiceUnavailable<T>();
//         }
//     }
// }