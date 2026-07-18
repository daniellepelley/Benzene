using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Discovery.Aws;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test.Discovery;

public class AwsLambdaDiscoveryProviderTest
{
    private static FunctionConfiguration Fn(string name)
        => new() { FunctionName = name, FunctionArn = $"arn:aws:lambda:eu-west-1:1:function:{name}" };

    private static Mock<IAmazonLambda> LambdaWith(
        IReadOnlyDictionary<string, (FunctionConfiguration Fn, Dictionary<string, string> Tags)> functionsByMarkerPage,
        params (string? Marker, string? NextMarker, string[] Names)[] pages)
    {
        var mock = new Mock<IAmazonLambda>();

        foreach (var page in pages)
        {
            var captured = page;
            mock.Setup(x => x.ListFunctionsAsync(
                    It.Is<ListFunctionsRequest>(r => r.Marker == captured.Marker), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListFunctionsResponse
                {
                    Functions = captured.Names.Select(n => functionsByMarkerPage[n].Fn).ToList(),
                    NextMarker = captured.NextMarker
                });
        }

        foreach (var (_, value) in functionsByMarkerPage)
        {
            var tags = value.Tags;
            var arn = value.Fn.FunctionArn;
            mock.Setup(x => x.ListTagsAsync(
                    It.Is<ListTagsRequest>(r => r.Resource == arn), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListTagsResponse { Tags = tags });
        }

        return mock;
    }

    [Fact]
    public async Task Discover_EmitsOnlyTaggedFunctions_AsAwsLambdaInvokeEntries()
    {
        var functions = new Dictionary<string, (FunctionConfiguration, Dictionary<string, string>)>
        {
            ["orders"] = (Fn("orders"), new Dictionary<string, string> { ["benzene"] = "true" }),
            ["unrelated"] = (Fn("unrelated"), new Dictionary<string, string> { ["team"] = "x" }), // no benzene tag
        };
        var mock = LambdaWith(functions, (null, null, new[] { "orders", "unrelated" }));

        var provider = new AwsLambdaDiscoveryProvider(mock.Object);
        var entries = await provider.DiscoverAsync(new MeshDiscoveryFilter());

        var entry = Assert.Single(entries);
        Assert.Equal("orders", entry.Name);
        Assert.Equal(MeshServiceSource.AwsLambdaInvoke, entry.Source);
        Assert.Equal("orders", entry.SourceOptions!["functionName"]);
    }

    [Fact]
    public async Task Discover_FollowsPaginationMarker()
    {
        var functions = new Dictionary<string, (FunctionConfiguration, Dictionary<string, string>)>
        {
            ["a"] = (Fn("a"), new Dictionary<string, string> { ["benzene"] = "1" }),
            ["b"] = (Fn("b"), new Dictionary<string, string> { ["benzene"] = "1" }),
        };
        var mock = LambdaWith(functions,
            (null, "page2", new[] { "a" }),
            ("page2", null, new[] { "b" }));

        var provider = new AwsLambdaDiscoveryProvider(mock.Object);
        var entries = await provider.DiscoverAsync(new MeshDiscoveryFilter());

        Assert.Equal(new[] { "a", "b" }, entries.Select(e => e.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task Discover_CarriesMeshPathHintTag()
    {
        var functions = new Dictionary<string, (FunctionConfiguration, Dictionary<string, string>)>
        {
            ["orders"] = (Fn("orders"), new Dictionary<string, string>
            {
                ["benzene"] = "true",
                ["benzene:mesh-path"] = "/custom/mesh"
            }),
        };
        var mock = LambdaWith(functions, (null, null, new[] { "orders" }));

        var provider = new AwsLambdaDiscoveryProvider(mock.Object);
        var entry = Assert.Single(await provider.DiscoverAsync(new MeshDiscoveryFilter()));

        Assert.Equal("/custom/mesh", entry.SourceOptions!["meshPath"]);
    }

    [Fact]
    public async Task Discover_ValuedTagFilter_ExcludesNonMatching()
    {
        var functions = new Dictionary<string, (FunctionConfiguration, Dictionary<string, string>)>
        {
            ["prod-svc"] = (Fn("prod-svc"), new Dictionary<string, string> { ["benzene"] = "prod" }),
            ["dev-svc"] = (Fn("dev-svc"), new Dictionary<string, string> { ["benzene"] = "dev" }),
        };
        var mock = LambdaWith(functions, (null, null, new[] { "prod-svc", "dev-svc" }));

        var provider = new AwsLambdaDiscoveryProvider(mock.Object);
        var entries = await provider.DiscoverAsync(
            new MeshDiscoveryFilter(new Dictionary<string, string?> { ["benzene"] = "prod" }));

        var entry = Assert.Single(entries);
        Assert.Equal("prod-svc", entry.Name);
    }
}
