using System;
using Benzene.Extras.Broadcast;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class EventServiceDocumentBuilderJsonTest
{
    private SchemaBuilder _schemaBuilder;
    private const string Topic = "tenant:created";

    [Fact]
    public void Json_NestedType()
    {
        var broadcastEventDefinition = new BroadcastEventDefinition(Topic, typeof(Example));

        var json = JsonConvert.SerializeObject(new Example
        {
            Title = "some-title",
            Value = 42,
            Inner = new[] {
                new Inner
                {
                    Title = Guid.NewGuid().ToString(),
                    Value = 42,
                    Date = new DateTime(2023, 1, 1, 0,0,0, DateTimeKind.Utc)
                }
            }
        });

        var doc = CreateBuilder()
            .AddBroadcastEventDefinitions(new[] { broadcastEventDefinition })
            .Build();

        var doc2 = CreateBuilder()
            .AddJsonEvent(Topic, "Example", json)
            .Build();

        var doc1Json = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        var doc2Json = doc2.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

        Assert.Equal(doc1Json, doc2Json);
    }


    [Fact]
    public void Json_SimpleType()
    {
        var broadcastEventDefinition = new BroadcastEventDefinition(Topic, typeof(Inner));

        var json = JsonConvert.SerializeObject(new Inner
        {
            Title = "some-title",
            Value = 42,
            Date = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var doc = CreateBuilder()
            .AddBroadcastEventDefinitions(new[] { broadcastEventDefinition })
            .Build();


        var doc2 = CreateBuilder()
            .AddJsonEvent(Topic, "Inner", json)
            .Build();

        var doc1Json = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        var doc2Json = doc2.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

        Assert.Equal(doc1Json, doc2Json);
    }

    private EventServiceDocumentBuilder CreateBuilder()
    {
        _schemaBuilder = new SchemaBuilder();
        return new EventServiceDocumentBuilder(_schemaBuilder)
            .AddInfo(new OpenApiInfo
            {
                Title = "platform-tenant-core-func",
                Version = "1.0",
                Description = "Core Tenant Data"
            })
            .AddTag(new OpenApiTag
            {
                Name = "Core Service"
            })
            .AddTag(new OpenApiTag
            {
                Name = "Platform"
            });
    }
}
