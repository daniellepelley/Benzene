using Benzene.Core.Broadcast;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.AsyncApi;
using LEGO.AsyncAPI;
using LEGO.AsyncAPI.Models;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class AsyncApiDocumentBuilderJsonTest
{
    private SchemaBuilder _schemaBuilder;
    private const string Topic = "tenant:created";
    private const string Topic2 = "tenant:updated";


    [Fact]
    public void Json_SimpleType()
    {
        var broadcastEventDefinition = new BroadcastEventDefinition(Topic, typeof(Inner));

        var json = JsonConvert.SerializeObject(new Inner
        {
            Title = "some-title",
            Value = 42,
            Date = new System.DateTime(2023, 1, 1)
        });

        var doc = CreateBuilder()
            .AddBroadcastEventDefinitions(new[] { broadcastEventDefinition })
            .Build();

        var doc2 = CreateBuilder()
            .AddJsonEvent(Topic, "Inner", json)
            .Build();

        var doc1Json = doc.SerializeAsJson(AsyncApiVersion.AsyncApi2_0);
        var doc2Json = doc2.SerializeAsJson(AsyncApiVersion.AsyncApi2_0);

        Assert.Equal(doc1Json, doc2Json);
    }

    [Fact]
    public void Json_NestedType()
    {
        var broadcastEventDefinition1 = new BroadcastEventDefinition(Topic, typeof(Example));
        var broadcastEventDefinition2 = new BroadcastEventDefinition(Topic2, typeof(Inner));

        var json = JsonConvert.SerializeObject(new Example
        {
            Title = "some-title",
            Value = 42,
            Inner = new[] {
                new Inner
                {
                    Title = "some-title",
                    Value = 42,
                    Date  = new System.DateTime(2023, 1, 1)
                }
            }
        });

        var json2 = JsonConvert.SerializeObject(new Inner
        {
            Title = "some-title",
            Value = 42,
            Date = new System.DateTime(2023, 1, 1)
        });

        var doc = CreateBuilder()
            .AddBroadcastEventDefinition(broadcastEventDefinition1)
            .AddBroadcastEventDefinition(broadcastEventDefinition2)
            .Build();

        var doc2 = CreateBuilder()
            .AddJsonEvent(Topic, "Example", json)
            .AddJsonEvent(Topic2, "Inner", json2)
            .Build();

        var doc1Json = doc.SerializeAsJson(AsyncApiVersion.AsyncApi2_0);
        var doc2Json = doc2.SerializeAsJson(AsyncApiVersion.AsyncApi2_0);

        Assert.Equal(doc1Json, doc2Json);
    }

    private AsyncApiDocumentBuilder CreateBuilder()
    {
        _schemaBuilder = new SchemaBuilder();
        return new AsyncApiDocumentBuilder(_schemaBuilder)
            .AddInfo(new AsyncApiInfo()
            {
                Title = "platform-tenant-core-func",
                Version = "1.0",
                Description = "Core Tenant Data"
            })
            .AddTag(new AsyncApiTag
            {
                Name = "Core Service"
            })
            .AddTag(new AsyncApiTag
            {
                Name = "Platform"
            });
    }
}
