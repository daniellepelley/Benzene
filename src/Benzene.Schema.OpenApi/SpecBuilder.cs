using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Validation;
using Benzene.Http.BenzeneMessage;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi.Abstractions;
using Benzene.Schema.OpenApi.AsyncApi;
using Benzene.Schema.OpenApi.EventService;
using Benzene.Schema.OpenApi.OpenApi;

namespace Benzene.Schema.OpenApi;

public class SpecBuilder
{
    public string CreateSpec(IServiceResolver resolver, SpecRequest specRequest)
    {
        var type = specRequest.Type?.ToLowerInvariant();
        var builder = CreateBuilder(resolver, type);

        return builder switch
        {
            IProducesYaml producesYaml when specRequest.Format == "yaml" => producesYaml.GenerateYaml(),
            IProducesJson producesJson => producesJson.GenerateJson(),
            _ => string.Empty
        };
    }
    
    public object CreateBuilder(IServiceResolver resolver, string? type)
    {
        return type switch
        {
            "openapi" => CreateSpec(resolver, new OpenApiDocumentBuilder(CreateSchemaBuilder(resolver))),
            "asyncapi" => CreateSpec(resolver, new AsyncApiDocumentBuilder(CreateSchemaBuilder(resolver),
                resolver.TryGetService<AsyncApiSpecOptions>()?.ResponseTopicSuffix)),
            "benzene" => CreateSpec(resolver, new EventServiceDocumentBuilder(CreateSchemaBuilder(resolver))),
            _ => CreateSpec(resolver, new EventServiceDocumentBuilder(CreateSchemaBuilder(resolver)))
        };
    }

    
    public TBuilder CreateSpec<TBuilder>(IServiceResolver resolver, TBuilder builder)
    {
        if (builder is IConsumesApplicationInfo<TBuilder> consumesApplicationInfo)
        {
            var applicationInfo = resolver.TryGetService<IApplicationInfo>();
            if (applicationInfo != null)
            {
                consumesApplicationInfo.AddApplicationInfo(applicationInfo);
            }
        }
        if (builder is IConsumesMessageHandlerDefinitions<TBuilder> consumesMessageHandlerDefinitions)
        {
            var messageHandlersFinder = resolver.TryGetService<IMessageHandlersFinder>();
            if (messageHandlersFinder != null)
            {
                consumesMessageHandlerDefinitions.AddMessageHandlerDefinitions(messageHandlersFinder.FindDefinitions());
            }
        }
        if (builder is IConsumesHttpEndpointDefinitions<TBuilder> consumesHttpEndpointDefinitions)
        {
            var messageHandlersFinder = resolver.TryGetService<IMessageHandlersFinder>();
            var httpEndpointFinder = resolver.TryGetService<IHttpEndpointFinder>();
            if (messageHandlersFinder != null && httpEndpointFinder != null)
            {
                consumesHttpEndpointDefinitions.AddHttpEndpointDefinitions(httpEndpointFinder.FindDefinitions(), messageHandlersFinder.FindDefinitions());
            }
        }
        if (builder is IConsumesBroadcastEventsDefinitions<TBuilder> consumesBroadcastEventsDefinitions)
        {
            var messageFinder = resolver.TryGetService<IMessageDefinitionFinder<IMessageDefinition>>();
            if (messageFinder != null)
            {
                consumesBroadcastEventsDefinitions.AddBroadcastEventDefinitions(messageFinder.FindDefinitions());
            }
        }
        if (builder is IConsumesMessageEndpoint<TBuilder> consumesMessageEndpoint)
        {
            var messageEndpointInfo = resolver.TryGetService<IBenzeneMessageHttpEndpointInfo>();
            if (messageEndpointInfo != null)
            {
                consumesMessageEndpoint.AddMessageEndpoint(messageEndpointInfo.Path);
            }
        }
        if (builder is IConsumesTransportsInfo<TBuilder> consumesTransportsInfo)
        {
            var transportsInfo = resolver.TryGetService<ITransportsInfo>();
            if (transportsInfo != null)
            {
                consumesTransportsInfo.AddTransportsInfo(transportsInfo);
            }
        }

        return builder;
    }

    private ISchemaBuilder CreateSchemaBuilder(IServiceResolver resolver)
    {
        // BYO seam: a registered ISchemaBuilder (e.g. SuppliedSchemaBuilder over hand-authored
        // schema documents) replaces the default reflection builder. Register custom builders
        // transient/scoped - a schema builder accumulates one document's components catalogue,
        // so a singleton would leak schemas across spec builds. Without one, reflection
        // generation honors any registered SchemaGenerationOptions (inheritance/polymorphism).
        var schemaBuilder = resolver.TryGetService<ISchemaBuilder>()
                            ?? new SchemaBuilder(resolver.TryGetService<SchemaGenerationOptions>());

        var validationSchemaBuilder = resolver.TryGetService<IValidationSchemaBuilder>();
        if (validationSchemaBuilder != null)
        {
            return new OpenApiValidationSchemaBuilder(schemaBuilder, validationSchemaBuilder);
        }

        return schemaBuilder;
    }
}
