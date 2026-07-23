using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// System.Text.Json source-generation context for the API Gateway (payload format 1.0) proxy event
/// types. It supplies compile-time-generated (de)serialization metadata for
/// <see cref="APIGatewayProxyRequest"/> and <see cref="APIGatewayProxyResponse"/>, so
/// <see cref="ApiGatewayLambdaHandler"/> reads the request and writes the response without
/// System.Text.Json building that metadata by reflection on the first invocation - the bulk of the
/// cold-start "API Gateway -> Benzene" conversion cost in the AWS X-Ray cold-start analysis. Public so
/// an app can reuse it (e.g. when moving the function toward trimming/Native AOT).
/// </summary>
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
public partial class ApiGatewayJsonSerializerContext : JsonSerializerContext
{
}
