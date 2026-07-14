using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;
using Benzene.Test.Examples;
using Moq;
using Xunit;
using Constants = Benzene.Core.MessageHandlers.Constants;

namespace Benzene.Test.Core.Core.Response;

public class ResponseHandlerContainerTest
{
    [Fact]
    public async Task HandleAsync()
    {
        var messageHandlerDefinition = Mother.CreateMessageHandlerDefinitionV2();

        var mediaFormatNegotiator = new MediaFormatNegotiator<BenzeneMessageContext>(
            Array.Empty<IMediaFormat<BenzeneMessageContext>>(),
            new JsonMediaFormat<BenzeneMessageContext>(new JsonSerializer()),
            Mock.Of<IServiceResolver>());

        var messageHandlerFactory = new ResponseHandlerContainer<BenzeneMessageContext>(new BenzeneMessageResponseAdapter(),
            new IResponseHandler<BenzeneMessageContext>[]
        {
            new DefaultResponseStatusHandler<BenzeneMessageContext> (new BenzeneMessageResponseAdapter()),
            new SerializationResponseHandler<BenzeneMessageContext>(
                new BenzeneMessageResponseAdapter(),
                new DefaultResponsePayloadMapper<BenzeneMessageContext>(),
                mediaFormatNegotiator,
                Mock.Of<IServiceResolver>())
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
