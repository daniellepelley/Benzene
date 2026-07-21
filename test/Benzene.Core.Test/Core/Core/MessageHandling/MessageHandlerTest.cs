using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MessageHandlerTest
{
    [Fact]
    public async Task HandlersMessage()
    {
        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload
            {
                Name = Defaults.Name
            });

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .ReturnsAsync(BenzeneResult.Ok<ExampleResponsePayload>());
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, NullLogger.Instance, new DefaultStatuses());

        var result = await messageHandler.HandleAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }
    
    [Fact]
    public async Task HandlersMessage_ArgumentException()
    {
        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload
            {
                Name = Defaults.Name
            });

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .Throws(new ArgumentException("Wrong Argument"));
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, NullLogger.Instance, new DefaultStatuses());

        var result = await messageHandler.HandleAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.ValidationError, result.Status);
    }
 
    [Fact]
    public async Task HandlersMessage_Null()
    {
        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(null as ExampleRequestPayload);

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .ReturnsAsync(BenzeneResult.Ok<ExampleResponsePayload>());
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, NullLogger.Instance, new DefaultStatuses());

        var result = await messageHandler.HandleAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }
   
    [Fact]
    public async Task HandlersMessage_HandlerError()
    {
        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload
            {
                Name = Defaults.Name
            });

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .Throws(new Exception("some-error"));
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, NullLogger.Instance, new DefaultStatuses());

        var result = await messageHandler.HandleAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }
                
    [Fact]
    public async Task HandlersMessage_SerializationError()
    {
        var errorMessage = "Invalid Format";
        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Throws(new SerializationException(errorMessage));

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .ReturnsAsync(BenzeneResult.Ok<ExampleResponsePayload>());
        
        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, NullLogger.Instance, new DefaultStatuses());

        var result = await messageHandler.HandleAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.BadRequest, result.Status);
        Assert.Equal(errorMessage, result.Errors[1]);
        mockMessageHandler.Verify(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()), Times.Never);
    }

    [Fact]
    public async Task HandlersMessage_HandlerThrewGenuineCancellation_Propagates()
    {
        // A genuine cancellation (host shutdown / drain) must NOT be swallowed into a ServiceUnavailable
        // result - it must propagate so ExceptionHandlerMiddleware can let the transport redeliver the
        // interrupted work rather than a settle/ack/checkpoint transport dropping it as "done".
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload { Name = Defaults.Name });

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, NullLogger.Instance, new DefaultStatuses());

        await Assert.ThrowsAsync<OperationCanceledException>(() => messageHandler.HandleAsync(mockRequestFactory.Object));
    }

    [Fact]
    public async Task HandlersMessage_DeserializeThrewGenuineCancellation_Propagates()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Throws(new OperationCanceledException(cts.Token));

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();

        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, NullLogger.Instance, new DefaultStatuses());

        await Assert.ThrowsAsync<OperationCanceledException>(() => messageHandler.HandleAsync(mockRequestFactory.Object));
    }

    [Fact]
    public async Task HandlersMessage_HandlerThrewNonCancellationOce_IsStillTreatedAsError()
    {
        // An OperationCanceledException whose token is NOT signaled is not a genuine host cancellation
        // (e.g. a spuriously-thrown OCE); it must still be handled as a failure, not propagated.
        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload { Name = Defaults.Name });

        var mockMessageHandler = new Mock<IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>>();
        mockMessageHandler.Setup(x => x.HandleAsync(It.IsAny<ExampleRequestPayload>()))
            .ThrowsAsync(new OperationCanceledException(CancellationToken.None));

        var messageHandler =
            new MessageHandler<ExampleRequestPayload, ExampleResponsePayload>(mockMessageHandler.Object, NullLogger.Instance, new DefaultStatuses());

        var result = await messageHandler.HandleAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }
}
