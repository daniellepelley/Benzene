using System.Threading.Tasks;
using Benzene.Aws.Core;
using Benzene.Aws.Core.BenzeneMessage;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Broadcast;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Benzene.Tools;
using Benzene.Tools.Aws;
using LEGO.AsyncAPI.Readers;
using Microsoft.OpenApi.Readers;
using Xunit;
using Benzene.Core.MessageHandling;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class SpecTest
{
    private AwsLambdaBenzeneTestHost CreateStandardHost()
    {
        return new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x
                .UsingBenzene(x => x
                    .AddBenzene()
                    .AddBenzeneMessage()
                    .AddBroadcastEvent()
                    .AddHttpMessageHandlers()
                    .SetApplicationInfo("Example App", "1.0", "Stuff")
                ))
            .Configure(app =>
            {
                app.UseBenzeneMessage(x => x
                    .UseProcessResponse()
                    .UseSpec()
                    .UseMessageHandlers()
                );
            })
            .BuildHost();
    }

    private AwsLambdaBenzeneTestHost CreateIncompleteHost()
    {
        return new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x
                .UsingBenzene(x => x
                    .AddBenzene()
                    .AddBenzeneMessage()
                    .SetApplicationInfo("Example App", "1.0", "Stuff")
                ))
            .Configure(app =>
            {
                app.UseBenzeneMessage(x => x
                    .UseProcessResponse()
                    .UseSpec()
                    .UseMessageHandlers()
                );
            })
            .BuildHost();
    }

    [Fact]
    public async Task OpenApi_Test()
    {
        var host = CreateStandardHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("openapi","json")));
        var document = new OpenApiStringReader().Read(response.Body, out _);

        Assert.Equal(2, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task AsyncApi_Test()
    {
        var host = CreateStandardHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("asyncapi","json")));
        var document = new AsyncApiStringReader().Read(response.Body, out _);

        Assert.Equal(6, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task BenzeneApi_Test()
    {
        var host = CreateStandardHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("benzene", "json")));
        var document = new EventServiceDocumentDeserializer().Deserialize(response.Body);

        Assert.Equal(6, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task OpenApi_MissingDependencies_Test()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("openapi","json")));
        var document = new OpenApiStringReader().Read(response.Body, out _);

        Assert.Equal(0, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task AsyncApi_MissingDependencies_Test()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("asyncapi", "json")));
        var document = new AsyncApiStringReader().Read(response.Body, out _);

        Assert.Equal(6, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task BenzeneApi_MissingDependencies_Test()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("benzene", "json")));
        var document = new EventServiceDocumentDeserializer().Deserialize(response.Body);

        Assert.Equal(6, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task BenzeneApi_InvalidFormatDefaultsToJson()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("benzene", "foo")));
        var document = new EventServiceDocumentDeserializer().Deserialize(response.Body);

        Assert.Equal(6, document.Components.Schemas.Count);
    }

    [Fact]
    public async Task BenzeneApi_InvalidTypeDefaultsToBenzene()
    {
        var host = CreateIncompleteHost();
        var response = await host.SendBenzeneMessageAsync(MessageBuilder.Create("spec", new SpecRequest("benzene", "foo")));
        var document = new EventServiceDocumentDeserializer().Deserialize(response.Body);

        Assert.Equal(6, document.Components.Schemas.Count);
    }
}
