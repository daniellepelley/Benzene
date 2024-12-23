// using Benzene.Abstractions.Mappers;
// using Benzene.Abstractions.Request;
// using Benzene.Core.Helper;
// using Benzene.Http;
// using Microsoft.AspNetCore.Http;
//
// namespace Benzene.AspNet.Core;
//
// public class AspNetRequestMapper : IRequestMapper<AspNetContext>
// {
//     private readonly IMessageHeadersMapper<AspNetContext> _headersToBodyMapper;
//     private readonly IRouteFinder _routeFinder;
//
//     public AspNetRequestMapper(IRouteFinder routeFinder, IHttpHeaderMappings httpHeaderMappings)
//     {
//         _routeFinder = routeFinder;
//         _headersToBodyMapper = new AspNetHeadersToBodyMapper(httpHeaderMappings);
//     }
//
//     public TRequest? GetBody<TRequest>(AspNetContext context) where TRequest : class
//     {
//         return System.Text.Json.JsonSerializer.Deserialize<TRequest>(GetBody(context));
//     }
//
//     public string GetBody(AspNetContext context)
//     {
//         var route = _routeFinder.Find(context.HttpContext.Request.Method, context.HttpContext.Request.Path);
//
//         if (route == null)
//         {
//             return null;
//         }
//
//         var dictionary = new Dictionary<string, object>();
//
//         DictionaryUtils.MapOnto(dictionary, _headersToBodyMapper.GetHeaders(context));
//         DictionaryUtils.MapOnto(dictionary, context.HttpContext.Request.Query.ToDictionary(x => x.Key, x => x.Value.First()));
//         DictionaryUtils.MapOnto(dictionary, _headersToBodyMapper.GetHeaders(context));
//         DictionaryUtils.MapOnto(dictionary, CleanUp(route.Parameters));
//
//         var json = StreamToString(context.HttpContext.Request);
//
//         // DictionaryUtils.MapOnto(dictionary, DictionaryUtils.JsonToDictionary(json));
//
//         return JsonConvert.SerializeObject(dictionary);
//     }
//
//     private string StreamToString(HttpRequest request)
//     {
//         try
//         {
//             if (request.Body == null)
//             {
//                 return "{}";
//             }
//
//             using var sr = new StreamReader(request.Body);
//             var json = sr.ReadToEndAsync().BenzeneResult;
//             return json;
//         }
//         catch (Exception ex)
//         {
//             return null;
//         }
//     }
//
//     private static IDictionary<string, object> CleanUp(IDictionary<string, object> source)
//     {
//         return source
//             .Where(x => !x.Value.ToString()!.StartsWith("{"))
//             .ToDictionary(x => x.Key, x => x.Value);
//     }
// }