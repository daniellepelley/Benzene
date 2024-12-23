using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Logging;
using Benzene.Core.MessageHandlers;
using Benzene.Results;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MessageHandlerTest
{
    [Fact]
    public async Task HandlersMessage()
    {
        var mockRequestFactory = new Mock<IRequestFactory>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload
            {
                Name = Defaults.Name
            });

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .ReturnsAsync(BenzeneResult.Ok<ExampleResponsePayload>());
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, BenzeneLogger.NullLogger);

        var result = await messageHandler.HandlerAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }
    
    [Fact]
    public async Task HandlersMessage_ArgumentException()
    {
        var mockRequestFactory = new Mock<IRequestFactory>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload
            {
                Name = Defaults.Name
            });

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .Throws(new ArgumentException("Wrong Argument"));
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, BenzeneLogger.NullLogger);

        var result = await messageHandler.HandlerAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.ValidationError, result.Status);
    }
 
    [Fact]
    public async Task HandlersMessage_Null()
    {
        var mockRequestFactory = new Mock<IRequestFactory>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(null as ExampleRequestPayload);

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .ReturnsAsync(BenzeneResult.Ok<ExampleResponsePayload>());
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, BenzeneLogger.NullLogger);

        var result = await messageHandler.HandlerAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }
   
    [Fact]
    public async Task HandlersMessage_HandlerError()
    {
        var mockRequestFactory = new Mock<IRequestFactory>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload
            {
                Name = Defaults.Name
            });

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .Throws(new Exception("some-error"));
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, BenzeneLogger.NullLogger);

        var result = await messageHandler.HandlerAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }
                
    [Fact]
    public async Task HandlersMessage_SerializationError()
    {
        var errorMessage = "Invalid Format";
        var mockRequestFactory = new Mock<IRequestFactory>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Throws(new SerializationException(errorMessage));

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .ReturnsAsync(BenzeneResult.Ok<ExampleResponsePayload>());
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, BenzeneLogger.NullLogger);

        var result = await messageHandler.HandlerAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.BadRequest, result.Status);
        Assert.Equal(errorMessage, result.Errors[1]);
        mockMessageHandler.Verify(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()), Times.Never);
    }

}
