using System.Threading.Tasks;
using Benzene.Abstractions.Response;
using Benzene.Core.DirectMessage;
using Benzene.Core.Response;
using Benzene.Core.Results;
using Benzene.Core.Serialization;
using Benzene.Results;
using Benzene.Test.Examples;
using Xunit;
using Constants = Benzene.Core.Constants;

namespace Benzene.Test.Core.Core.Response;

public class ResponseHandlerContainerTest
{
    [Fact]
    public async Task HandleAsync()
    {
        var messageHandlerDefinition = Mother.CreateMessageHandlerDefinitionV2();

        var messageHandlerFactory = new ResponseHandlerContainer<DirectMessageContext>(new ISyncResponseHandler<DirectMessageContext>[]
        {
            new DefaultResponseStatusHandler<DirectMessageContext> (new DirectMessageResponseAdapter()),
            new ResponseBodyHandler<DirectMessageContext>(
                new DirectMessageResponseAdapter(),
                new DefaultResponsePayloadMapper<DirectMessageContext>(),
                new JsonSerializer())
        });

        var request = Mother.CreateRequest();
        var expected = new JsonSerializer().Serialize(request);

        var context = DirectMessageContext.CreateInstance(new DirectMessageRequest());
        context.MessageResult = new MessageResult(Defaults.Topic, messageHandlerDefinition,
            ServiceResultStatus.Ok, true, request, null);
        await messageHandlerFactory.HandleAsync(context);

        Assert.Equal(Constants.JsonContentType, context.DirectMessageResponse.Headers[Constants.ContentTypeHeader]);
        Assert.Equal(expected, context.DirectMessageResponse.Message);
        Assert.Equal(ServiceResultStatus.Ok, context.DirectMessageResponse.StatusCode);
    }
}
