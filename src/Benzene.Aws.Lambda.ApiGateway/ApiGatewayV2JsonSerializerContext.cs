using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// System.Text.Json source-generation context for the API Gateway HTTP API (payload format 2.0) proxy
/// event types, so <see cref="ApiGatewayV2LambdaHandler"/> reads the request and writes the response
/// without System.Text.Json building that metadata by reflection on the first (cold) invocation. Kept
/// separate from <see cref="ApiGatewayJsonSerializerContext"/> because the v1 and v2 request types have
/// nested types with the same simple name (e.g. <c>ProxyRequestContext</c>), which can't coexist in a
/// single source-generated context. Public so an app can reuse it (e.g. toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
public partial class ApiGatewayV2JsonSerializerContext : JsonSerializerContext
{
}
