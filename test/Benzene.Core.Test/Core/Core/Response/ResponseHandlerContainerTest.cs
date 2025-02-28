﻿using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;
using Benzene.Test.Examples;
using Xunit;
using Constants = Benzene.Core.MessageHandlers.Constants;

namespace Benzene.Test.Core.Core.Response;

public class ResponseHandlerContainerTest
{
    [Fact]
    public async Task HandleAsync()
    {
        var messageHandlerDefinition = Mother.CreateMessageHandlerDefinitionV2();

        var messageHandlerFactory = new ResponseHandlerContainer<BenzeneMessageContext>(new BenzeneMessageResponseAdapter(),
            new ISyncResponseHandler<BenzeneMessageContext>[]
        {
            new DefaultResponseStatusHandler<BenzeneMessageContext> (new BenzeneMessageResponseAdapter()),
            new ResponseBodyHandler<BenzeneMessageContext>(
                new BenzeneMessageResponseAdapter(),
                new DefaultResponsePayloadMapper<BenzeneMessageContext>(),
                new JsonSerializer())
        });

        var request = Mother.CreateRequest();
        var expected = new JsonSerializer().Serialize(request);

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        // context.MessageResult = new MessageResult(new Topic(Defaults.Topic), messageHandlerDefinition,
            // BenzeneResultStatus.Ok, true, request, null);
        await messageHandlerFactory.HandleAsync(context, new MessageHandlerResult(new Topic(Defaults.Topic), messageHandlerDefinition, BenzeneResult.Ok(request)));

        Assert.Equal(Constants.JsonContentType, context.BenzeneMessageResponse.Headers[Constants.ContentTypeHeader]);
        Assert.Equal(expected, context.BenzeneMessageResponse.Body);
        Assert.Equal(BenzeneResultStatus.Ok, context.BenzeneMessageResponse.StatusCode);
    }
}
