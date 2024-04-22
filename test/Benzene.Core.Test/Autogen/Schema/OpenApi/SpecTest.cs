using System.Threading.Tasks;
using Benzene.Aws.Core;
using Benzene.Aws.Core.BenzeneMessage;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Elements.Core.Broadcast;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Benzene.Tools;
using Benzene.Tools.Aws;
using LEGO.AsyncAPI.Readers;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class SpecTest
{
    private AwsLambdaBenzeneTestHost CreateStandardHost()
    {
        return new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x
                .UsingBenzene(x => x
                    .AddBenzeneMessage()
                    .AddBroadcastEvent()
                    .AddHttpMessageHandlers()
                    .SetApplicationInfo("Example App", "1.0", "Stuff")
                ))
            .Configure(app =>
            {
                app.UseBenzeneMessage(x => x
                    .UseProcessResponse()
                    .UseSpec("spec")
                    .UseMessageRouter()
                );
            })
            .BuildHost();
    }

    private AwsLambdaBenzeneTestHost CreateIncompleteHost()
    {
        return new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x
                .UsingBenzene(x => x
                    .AddBenzeneMessage()
                    .SetApplicationInfo("Example App", "1.0", "Stuff")
                ))
            .Configure(app =>
            {
                app.UseBenzeneMessage(x => x
                    .UseProcessResponse()
                    .UseSpec("spec")
                    .UseMessageRouter()
                );
            })
            .BuildHost();
    }

    [Fact]
    public async Task OpenApi_Test()
    {
        var host = CreateStandardHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("openapi","json")));
        var document = new OpenApiStringReader().Read(response.Message, out _);

        Assert.Equal(2, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task AsyncApi_Test()
    {
        var host = CreateStandardHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("asyncapi","json")));
        var document = new AsyncApiStringReader().Read(response.Message, out _);

        Assert.Equal(4, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task BenzeneApi_Test()
    {
        var host = CreateStandardHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("iris", "json")));
        var document = new EventServiceDocumentDeserializer().Deserialize(response.Message);

        Assert.Equal(4, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task OpenApi_MissingDependencies_Test()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("openapi","json")));
        var document = new OpenApiStringReader().Read(response.Message, out _);

        Assert.Equal(0, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task AsyncApi_MissingDependencies_Test()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("asyncapi", "json")));
        var document = new AsyncApiStringReader().Read(response.Message, out _);

        Assert.Equal(4, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task BenzeneApi_MissingDependencies_Test()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("iris", "json")));
        var document = new EventServiceDocumentDeserializer().Deserialize(response.Message);

        Assert.Equal(4, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task BenzeneApi_InvalidFormatDefaultsToJson()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("iris", "foo")));
        var document = new EventServiceDocumentDeserializer().Deserialize(response.Message);

        Assert.Equal(4, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task BenzeneApi_InvalidTypeDefaultsToBenzene()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("foo", "foo")));
        var document = new EventServiceDocumentDeserializer().Deserialize(response.Message);

        Assert.Equal(4, document.Components.Schemas.Count);
    }
}
