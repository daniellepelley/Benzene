using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.CodeGen.Client;
using Benzene.Core.MessageHandlers;
using Benzene.Results;
using Benzene.Schema.OpenApi.EventService;
using Xunit;

namespace Benzene.Examples.CodeGen.Client;

public class HelloWorldMessage
{
    public string Name { get; set; }
}

public class HelloWorldResponse
{
    public string Message { get; set; }
}

[Message("hello:world")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldMessage message)
    {
        return BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}" }).AsTask();
    }
}

public class GeneratesClientSdkFromMessageHandlersTest
{
    [Fact]
    public void GeneratesClientSdk_ForDiscoveredMessageHandlers()
    {
        var messageHandlerDefinitions = new ReflectionMessageHandlersFinder(typeof(HelloWorldMessageHandler).Assembly)
            .FindDefinitions();

        var eventServiceDocument = messageHandlerDefinitions.ToEventServiceDocument();

        var sdkBuilder = new MessageClientSdkBuilder("HelloWorld", "Benzene.Examples.Clients");
        var codeFiles = sdkBuilder.BuildCodeFiles(eventServiceDocument);

        var clientFile = codeFiles.Single(x => x.Name == "HelloWorldServiceClient.cs");
        Assert.Contains("HelloWorldServiceClient", string.Join('\n', clientFile.Lines));
    }
}
