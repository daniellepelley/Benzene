using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Schema.OpenApi.TestPayloads;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi.TestPayloads;

public class TestPayloadsBuilderTest
{
    private static Benzene.Schema.OpenApi.EventService.EventServiceDocument DocumentWith(
        string[] transports, params (string Topic, Type Request, Type Response)[] topics)
    {
        var document = topics
            .ToDictionary(t => t.Topic, t => (t.Request, t.Request, t.Response))
            .ToEventServiceDocument();
        document.Transports = transports;
        return document;
    }

    [Fact]
    public void EmitsBenzeneMessagePayload_PerDomainTopic()
    {
        var document = DocumentWith(
            new[] { "sqs" },
            ("user:get", typeof(GetUserMessage), typeof(UserDto)),
            ("user:create", typeof(CreateUserMessage), typeof(string)));

        var manifest = new TestPayloadsBuilder().Build(document);

        Assert.Equal(2, manifest.Topics.Length);
        var getTopic = manifest.Topics.Single(t => t.Topic == "user:get");
        Assert.Contains("sqs", getTopic.Transports);
        var payload = Assert.IsType<BenzeneMessagePayload>(getTopic.Payloads["benzene-message"]);
        Assert.Equal("user:get", payload.Topic);
        // The body is a JSON-serialized example object - valid JSON, ready to POST to /benzene-message.
        var body = JObject.Parse(payload.Body);
        Assert.NotNull(body);
    }

    [Fact]
    public void SkipsReservedUtilityTopics()
    {
        var document = DocumentWith(
            Array.Empty<string>(),
            ("user:get", typeof(GetUserMessage), typeof(UserDto)),
            ("spec", typeof(GetUserMessage), typeof(UserDto)));

        var manifest = new TestPayloadsBuilder().Build(document);

        Assert.Single(manifest.Topics);
        Assert.Equal("user:get", manifest.Topics[0].Topic);
    }

    [Fact]
    public void TopicFilter_RestrictsToOneTopic()
    {
        var document = DocumentWith(
            Array.Empty<string>(),
            ("user:get", typeof(GetUserMessage), typeof(UserDto)),
            ("user:create", typeof(CreateUserMessage), typeof(string)));

        var manifest = new TestPayloadsBuilder().Build(document, "user:create");

        Assert.Single(manifest.Topics);
        Assert.Equal("user:create", manifest.Topics[0].Topic);
    }

    [Fact]
    public void BuildJson_IsValidCamelCasedJson()
    {
        var document = DocumentWith(
            new[] { "sqs" },
            ("user:get", typeof(GetUserMessage), typeof(UserDto)));

        var json = new TestPayloadsBuilder().BuildJson(document);

        var parsed = JObject.Parse(json);
        Assert.NotNull(parsed["topics"]);
        Assert.Equal("user:get", (string)parsed["topics"]![0]!["topic"]!);
        // camelCased envelope key round-trips.
        Assert.NotNull(parsed["topics"]![0]!["payloads"]!["benzene-message"]!["body"]);
    }
}
