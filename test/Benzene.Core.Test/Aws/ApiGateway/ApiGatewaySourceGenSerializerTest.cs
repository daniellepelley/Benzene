using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

/// <summary>
/// ApiGatewayLambdaHandler swaps the base router's reflection-based DefaultLambdaJsonSerializer for a
/// source-generated one (ApiGatewayJsonSerializerContext) to keep System.Text.Json's per-type metadata
/// build off the first (cold) invocation. These tests pin down that the swap is behaviour-preserving -
/// the source-generated serializer reads the request and writes the response identically to the
/// reflection serializer - and that the handler is actually wired to it.
/// </summary>
public class ApiGatewaySourceGenSerializerTest
{
    private static readonly ILambdaSerializer Reflection = new DefaultLambdaJsonSerializer();
    private static readonly ILambdaSerializer SourceGen = new SourceGeneratorLambdaJsonSerializer<ApiGatewayJsonSerializerContext>();

    // A representative API Gateway (payload format 1.0) proxy event as it arrives on the wire.
    private const string ProxyRequestJson =
        "{\"resource\":\"/orders\",\"path\":\"/orders\",\"httpMethod\":\"POST\"," +
        "\"headers\":{\"content-type\":\"application/json\",\"benzene-topic\":\"orders:create\"}," +
        "\"queryStringParameters\":{\"dryRun\":\"true\"}," +
        "\"body\":\"{\\\"item\\\":\\\"Espresso Machine\\\",\\\"quantity\\\":2}\"," +
        "\"isBase64Encoded\":false}";

    private static T Deserialize<T>(ILambdaSerializer serializer, string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return serializer.Deserialize<T>(stream);
    }

    private static string Serialize<T>(ILambdaSerializer serializer, T value)
    {
        using var stream = new MemoryStream();
        serializer.Serialize(value, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact]
    public void SourceGen_DeserializesProxyRequest_IdenticallyToReflection()
    {
        var reflection = Deserialize<APIGatewayProxyRequest>(Reflection, ProxyRequestJson);
        var sourceGen = Deserialize<APIGatewayProxyRequest>(SourceGen, ProxyRequestJson);

        Assert.Equal(reflection.HttpMethod, sourceGen.HttpMethod);
        Assert.Equal(reflection.Path, sourceGen.Path);
        Assert.Equal(reflection.Body, sourceGen.Body);
        Assert.Equal(reflection.IsBase64Encoded, sourceGen.IsBase64Encoded);
        Assert.Equal(reflection.Headers["content-type"], sourceGen.Headers["content-type"]);
        Assert.Equal(reflection.Headers["benzene-topic"], sourceGen.Headers["benzene-topic"]);
        Assert.Equal(reflection.QueryStringParameters["dryRun"], sourceGen.QueryStringParameters["dryRun"]);
    }

    [Fact]
    public void SourceGen_SerializesProxyResponse_IdenticallyToReflection()
    {
        var response = new APIGatewayProxyResponse
        {
            StatusCode = 201,
            Body = "{\"id\":\"ord-1a2b\"}",
            Headers = new Dictionary<string, string> { { "content-type", "application/json" } },
            IsBase64Encoded = false
        };

        // Both serializers apply the same Amazon Lambda options, so the wire bytes should match exactly.
        Assert.Equal(Serialize(Reflection, response), Serialize(SourceGen, response));
    }

    private sealed class ExposingApiGatewayHandler : ApiGatewayLambdaHandler
    {
        public ExposingApiGatewayHandler()
            : base(new NullMiddlewarePipeline<ApiGatewayContext>(), new NullServiceResolver())
        {
        }

        public ILambdaSerializer Serializer => JsonSerializer;
    }

    private sealed class NullMiddlewarePipeline<TContext> : Benzene.Abstractions.Middleware.IMiddlewarePipeline<TContext>
    {
        public System.Threading.Tasks.Task HandleAsync(TContext context, Benzene.Abstractions.DI.IServiceResolver serviceResolver)
            => System.Threading.Tasks.Task.CompletedTask;
    }

    [Fact]
    public void ApiGatewayLambdaHandler_UsesTheSourceGeneratedSerializer()
    {
        var serializer = new ExposingApiGatewayHandler().Serializer;

        Assert.IsType<SourceGeneratorLambdaJsonSerializer<ApiGatewayJsonSerializerContext>>(serializer);
    }
}
