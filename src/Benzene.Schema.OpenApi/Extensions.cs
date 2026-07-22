using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Schema.OpenApi.AsyncApi;
using Benzene.Schema.OpenApi.TestPayloads;

namespace Benzene.Schema.OpenApi;

public static class Extensions
{
    /// <summary>
    /// Overrides the suffix used to name a handled topic's reply channel in the generated AsyncAPI
    /// document (default <see cref="AsyncApiDocumentBuilder.DefaultResponseTopicSuffix"/>, i.e.
    /// <c>&lt;topic&gt;:response</c>). For example, passing <c>"reply"</c> makes a handler on
    /// <c>shipping:get-all</c> reply on <c>shipping:get-all:reply</c>.
    /// </summary>
    public static IBenzeneServiceContainer SetAsyncApiResponseTopicSuffix(
        this IBenzeneServiceContainer services, string responseTopicSuffix)
    {
        services.AddSingleton(new AsyncApiSpecOptions { ResponseTopicSuffix = responseTopicSuffix });
        return services;
    }

    /// <summary>
    /// Opts spec schema generation into inheritance (<c>allOf</c>) and/or polymorphism
    /// (<c>oneOf</c> + discriminator) rendering — see <see cref="SchemaGenerationOptions"/>.
    /// Without this call, generated schemas keep the default flattened shape.
    /// </summary>
    public static IBenzeneServiceContainer SetSchemaGenerationOptions(
        this IBenzeneServiceContainer services, SchemaGenerationOptions options)
    {
        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Registers hand-authored (bring-your-own) payload schemas: the spec serves the catalog's
    /// schemas for the types it maps and falls back to reflection generation (honoring any
    /// registered <see cref="SchemaGenerationOptions"/>) for everything else. Registered transient
    /// because a schema builder accumulates one document's components catalogue.
    /// </summary>
    public static IBenzeneServiceContainer AddSuppliedSchemas(
        this IBenzeneServiceContainer services, SuppliedSchemaCatalog catalog)
    {
        services.AddSingleton(catalog);
        services.AddTransient<ISchemaBuilder>(x =>
            new SuppliedSchemaBuilder(catalog, new SchemaBuilder(x.TryGetService<SchemaGenerationOptions>())));
        return services;
    }

    /// <summary>
    /// Registers the <c>test-payloads</c> handler, which serves ready-to-fire example payloads for the
    /// service's domain topics (see <see cref="TestPayloadsMessageHandler"/>). Opt-in by design - like
    /// <see cref="UseSpec{TContext}(IMiddlewarePipelineBuilder{TContext})"/>, nothing is exposed unless
    /// this is called - and it reveals no more than the already-public <c>spec</c> topic.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseTestPayloads<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app.UseTestPayloads(Constants.DefaultTestPayloadsTopic);
    }

    /// <summary>Registers the test-payloads handler on the given topic.</summary>
    public static IMiddlewarePipelineBuilder<TContext> UseTestPayloads<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string topic)
    {
        app.Register(x =>
        {
            x.AddSingleton<IMessageHandlerDefinition>(_ =>
                MessageHandlerDefinition.CreateInstance(topic, "", typeof(TestPayloadsRequest),
                    typeof(RawStringMessage),
                    typeof(TestPayloadsMessageHandler)));
            x.AddScoped<TestPayloadsMessageHandler>();
        });
        return app;
    }

    public static IMiddlewarePipelineBuilder<TContext> UseSpec<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app.UseSpec(Constants.DefaultSpecTopic);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseSpec<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string topic)
    {
        app.Register(x =>
        {
            x.AddSingleton<IMessageHandlerDefinition>(_ =>
                MessageHandlerDefinition.CreateInstance(Constants.DefaultSpecTopic, "", typeof(SpecRequest),
                    typeof(RawStringMessage),
                    typeof(SpecMessageHandler)));
            x.AddScoped<SpecMessageHandler>();
            // Memoize the generated spec per (type, format) - it's deterministic for the process
            // lifetime, so repeated polling (e.g. the mesh aggregator) doesn't re-run the full
            // schema-generation build each time. TryAdd so a second UseSpec call is a no-op.
            x.TryAddSingleton<SpecCache>();
        });
        return app;
    }

    // public static IMiddlewarePipelineBuilder<TContext> UseSpec<TContext>(
    //     this IMiddlewarePipelineBuilder<TContext> app, string topic)
    // {
    //     return app.Use("Spec", async (resolver, context, next) =>
    //     {
    //         var mapper = resolver.GetService<IMessageGetter<TContext>>();
    //         var messageTopic = mapper.GetTopic(context);
    //
    //         if (messageTopic.Id == topic || messageTopic.Id == Constants.DefaultSpecTopic)
    //         {
    //             var specRequest = JsonConvert.DeserializeObject<SpecRequest>(mapper.GetBody(context));
    //
    //             var output = CreateSpec(resolver, specRequest ?? new SpecRequest("asyncapi", "json"));
    //
    //             context.MessageResult =
    //                 new MessageResult(
    //                     topic,
    //                     MessageHandlerDefinition.Empty(),
    //                     BenzeneResultStatus.Ok,
    //                     true,
    //                     new RawStringMessage(output),
    //                     Array.Empty<string>()
    //                 );
    //         }
    //         else
    //         {
    //             await next();
    //         }
    //     });
    // }

    private static string CreateSpec(IServiceResolver resolver, SpecRequest specRequest)
    {
        var specBuilder = new SpecBuilder();
        return specBuilder.CreateSpec(resolver, specRequest);
    }
}
