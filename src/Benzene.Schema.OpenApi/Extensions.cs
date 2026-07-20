using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Schema.OpenApi.TestPayloads;

namespace Benzene.Schema.OpenApi;

public static class Extensions
{
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
