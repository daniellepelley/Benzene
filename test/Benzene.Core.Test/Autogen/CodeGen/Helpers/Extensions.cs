using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.MessageHandling;
using Benzene.CodeGen.Core;
using Benzene.Core.MessageHandling;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi.Models;

namespace Benzene.Test.Autogen.CodeGen.Helpers;

public static class Extensions
{
    public static IDictionary<string, string> Build(this ICodeBuilder<EventServiceDocument> source,
        IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        return source.Build(messageHandlerDefinitions.ToEventServiceDocument());
    }

    public static IDictionary<string, string> Build<T>(this ICodeBuilder<T> codeBuilder, T source)
    {
        return codeBuilder.BuildCodeFiles(source)
            .ToFilesDictionary();
    }
    public static string ToText(this string[] source)
    {
        return string.Join("", source.Select(x => $"{x}{Environment.NewLine}"));
    }

    public static EventServiceDocument ToEventServiceDocument(
        this IDictionary<string, (Type, Type, Type)> dictionary)
    {
        return dictionary.ToMessageHandlerDefinitions().ToEventServiceDocument();
    }

    public static EventServiceDocument ToEventServiceDocument(
        this IDictionary<string, (Type, Type, Type)> dictionary, ISchemaBuilder schemaBuilder)
    {
        return dictionary.ToMessageHandlerDefinitions().ToEventServiceDocument(schemaBuilder);
    }

    public static IDictionary<string, OpenApiSchema> ToOpenApiSchemas(
        this IDictionary<string, (Type, Type, Type)> dictionary)
    {
        return dictionary.ToMessageHandlerDefinitions().ToEventServiceDocument().Components.Schemas;
    }

    public static IDictionary<string, OpenApiSchema> ToOpenApiSchemas(this IDictionary<string, (Type, Type, Type)> dictionary, ISchemaBuilder schemaBuilder)
    {
        return dictionary.ToMessageHandlerDefinitions().ToEventServiceDocument(schemaBuilder).Components.Schemas;
    }

    public static IMessageHandlerDefinition[] ToMessageHandlerDefinitions(
        this IDictionary<string, (Type, Type, Type)> dictionary)
    {
        return dictionary.Select(x =>
            MessageHandlerDefinition.CreateInstance(
                x.Key,
                x.Value.Item2,
                x.Value.Item3,
                x.Value.Item1
            ) as IMessageHandlerDefinition).ToArray();
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
