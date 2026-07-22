using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Schema.OpenApi.TestPayloads;

namespace Benzene.Aws.Lambda.TestPayloads;

/// <summary>
/// Dresses a topic's example payload as an <see cref="APIGatewayProxyRequest"/> - the shape an
/// API-Gateway-triggered Lambda receives - ready to paste into the Lambda test console. Skips topics
/// with no HTTP mappings. Mirrors the <c>api-gateway</c> Lambda-test-tool dressing
/// (<c>HttpBuilder.AsApiGatewayRequest()</c>).
/// </summary>
public class ApiGatewayTestPayloadDresser : ITestPayloadDresser
{
    /// <inheritdoc />
    public string Transport => "api-gateway";

    /// <inheritdoc />
    public object? Dress(TestPayloadDressingContext context)
    {
        if (context.HttpMappings.Count == 0)
        {
            return null;
        }

        // A topic can map to several routes; dress the first as a representative request. The manifest
        // is a "here's a valid call" aid, not an exhaustive route catalogue - HttpMappings carries them all.
        var mapping = context.HttpMappings[0];

        var request = new APIGatewayProxyRequest
        {
            HttpMethod = mapping.Method,
            Path = mapping.Path,
            Body = context.SerializedBody,
            Headers = new Dictionary<string, string>(context.Headers),
        };

        return AwsEventJson.ToToken(request);
    }
}
