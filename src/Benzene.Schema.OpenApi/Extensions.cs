﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandling;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Core.Results;
using Benzene.Results;
using Newtonsoft.Json;

namespace Benzene.Schema.OpenApi;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<THasMessageResult> UseSpec<THasMessageResult>(
        this IMiddlewarePipelineBuilder<THasMessageResult> app)
        where THasMessageResult : IHasMessageResult
    {
        return app.UseSpec(Constants.DefaultSpecTopic);
    }

    public static IMiddlewarePipelineBuilder<THasMessageResult> UseSpec<THasMessageResult>(
        this IMiddlewarePipelineBuilder<THasMessageResult> app, string topic)
        where THasMessageResult : IHasMessageResult
    {
        return app.Use("Spec", async (resolver, context, next) =>
        {
            var mapper = resolver.GetService<IMessageMapper<THasMessageResult>>();
            var messageTopic = mapper.GetTopic(context);

            if (messageTopic.Id == topic || messageTopic.Id == Constants.DefaultSpecTopic)
            {
                var specRequest = JsonConvert.DeserializeObject<SpecRequest>(mapper.GetMessage(context));

                var output = CreateSpec(resolver, specRequest ?? new SpecRequest("asyncapi", "json"));

                context.MessageResult =
                    new MessageResult(
                        topic,
                        MessageHandlerDefinition.Empty(),
                        ServiceResultStatus.Ok,
                        true,
                        new RawStringMessage(output),
                        Array.Empty<string>()
                    );
            }
            else
            {
                await next();
            }
        });
    }

    private static string CreateSpec(IServiceResolver resolver, SpecRequest specRequest)
    {
        var specBuilder = new SpecBuilder();
        return specBuilder.CreateSpec(resolver, specRequest);
    }
}
