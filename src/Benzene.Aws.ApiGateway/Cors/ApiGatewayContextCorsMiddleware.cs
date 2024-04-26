// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Benzene.Abstractions.Middleware;
// using Benzene.Aws.ApiGateway;
// using Benzene.Http.Cors;
// using Benzene.Http.Routing;
//
// public class ApiGatewayContextCorsMiddleware : IMiddleware<ApiGatewayContext>
// {
//     private readonly CorsSettings _corsSettings;
//     private readonly IHttpEndpointFinder _httpEndpointFinder;
//     private readonly UrlMatcher _urlMatcher;
//     public string Name => "Cors";
//
//     public ApiGatewayContextCorsMiddleware(CorsSettings corsSettings, IHttpEndpointFinder httpEndpointFinder)
//     {
//         _httpEndpointFinder = httpEndpointFinder;
//         _corsSettings = corsSettings;
//         _urlMatcher = new UrlMatcher();
//     }
//
//     public async Task HandleAsync(ApiGatewayContext context, Func<Task> next)
//     {
//         if (context.ApiGatewayProxyRequest.HttpMethod.ToLowerInvariant() != "options")
//         {
//             await next();
//         }
//
//         AddCorsHeaders(context);
//     }
//
//     private void AddCorsHeaders(ApiGatewayContext context)
//     {
//         if (context.ApiGatewayProxyRequest.Headers.Any(header => header.Key.ToLowerInvariant() == "origin"))
//         {
//             context.EnsureResponseExists();
//             context.ApiGatewayProxyResponse.StatusCode = 200;
//
//             var methods = FindMethods(context.ApiGatewayProxyRequest.Path);
//
//             if (!methods.Any())
//             {
//                 return;
//             }
//
//             var origin = context.ApiGatewayProxyRequest.Headers["origin"];
//
//             if (_corsSettings.AllowedDomains.Contains(origin))
//             {
//                 context.ApiGatewayProxyResponse.Headers.Add("access-control-allow-origin", origin);
//                 context.ApiGatewayProxyResponse.Headers.Add("access-control-allow-headers",
//                     string.Join(",", _corsSettings.AllowedHeaders));
//                 context.ApiGatewayProxyResponse.Headers.Add("access-control-allow-methods",
//                     "OPTIONS," + string.Join(",", methods));
//             }
//         }
//     }
//
//     private string[] FindMethods(string path)
//     {
//         var output = new List<string>();
//         var routes = _httpEndpointFinder.FindDefinitions();
//         foreach (var route in routes)
//         {
//             var parameters = _urlMatcher.MatchUrl(path, route.Path);
//
//             if (parameters != null)
//             {
//                 output.Add(route.Method.ToUpperInvariant());
//             }
//         }
//
//         return output.ToArray();
//     }
// }
