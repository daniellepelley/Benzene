using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;

namespace Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;

/// <summary>
/// System.Text.Json source-generation context for the API Gateway custom authorizer event types, so
/// <see cref="ApiGatewayCustomAuthorizerLambdaHandler"/> reads the request and writes the policy
/// response without System.Text.Json building that metadata by reflection on the first (cold)
/// invocation. Kept separate from the proxy contexts to avoid nested-type simple-name collisions in a
/// single source-generated context. Public so an app can reuse it (e.g. toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(APIGatewayCustomAuthorizerRequest))]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerResponse))]
public partial class ApiGatewayCustomAuthorizerJsonSerializerContext : JsonSerializerContext
{
}
