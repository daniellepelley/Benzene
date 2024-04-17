using Benzene.Abstractions.MessageHandling;
using Benzene.Http;

namespace Benzene.Schema.OpenApi.EventService;

public static class EventServiceDocumentBuilderExtensions
{
    public static EventServiceDocumentBuilder AddJsonEvent(this EventServiceDocumentBuilder source, string topic, string typeName, string json)
    {
        var jsonOpenApiSchemaBuilder = new JsonOpenApiSchemaBuilder();
    
        var schemas = jsonOpenApiSchemaBuilder.CreateSchema(typeName, json);
        source.AddEventDefinition(topic, typeName, schemas[typeName]);

        foreach (var schema in schemas.Where(x => x.Key != typeName))
        {
            source.AddSchema(schema.Key, schema.Value);
        }
             
        return source;
    }
    
    public static EventServiceDocument ToEventServiceDocument(this IMessageHandlerDefinition messageHandlerDefinition)
    {
        return new[] { messageHandlerDefinition }.ToEventServiceDocument(new SchemaBuilder());
    }

    public static EventServiceDocument ToEventServiceDocument(this IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        return messageHandlerDefinitions.ToEventServiceDocument(new SchemaBuilder());
    }

    public static EventServiceDocument ToEventServiceDocument(this IMessageHandlerDefinition[] messageHandlerDefinitions, ISchemaBuilder schemaBuilder)
    {
        var builder = new EventServiceDocumentBuilder(schemaBuilder);
        return builder.AddMessageHandlerDefinitions(messageHandlerDefinitions).Build();
    }
    
    public static EventServiceDocument ToEventServiceDocument(this IHttpEndpointDefinition httpEndpointDefinition, IMessageHandlerDefinition messageHandlerDefinition)
    {
        return new[] { httpEndpointDefinition }.ToEventServiceDocument(new []{ messageHandlerDefinition }, new SchemaBuilder());
    }
    
    public static EventServiceDocument ToEventServiceDocument(this IHttpEndpointDefinition[] httpEndpointDefinition, IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        return httpEndpointDefinition.ToEventServiceDocument(messageHandlerDefinitions, new SchemaBuilder());
    }

    public static EventServiceDocument ToEventServiceDocument(this IHttpEndpointDefinition[] httpEndpointDefinition, IMessageHandlerDefinition[] messageHandlerDefinitions, ISchemaBuilder schemaBuilder)
    {
        var builder = new EventServiceDocumentBuilder(schemaBuilder);
        return builder.AddHttpEndpointDefinitions(httpEndpointDefinition, messageHandlerDefinitions).Build();
    }
}
