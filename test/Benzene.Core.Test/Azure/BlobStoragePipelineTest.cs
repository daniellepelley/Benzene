using System;
using System.Text;
using System.Threading.Tasks;
using Benzene.Azure.Function.BlobStorage;
using Benzene.Azure.Function.Core;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class BlobStoragePipelineTest
{
    [Fact]
    public async Task Blob_IsDeliveredToThePipeline()
    {
        BlobStorageContext observed = null;

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseBlobStorage(blob => blob
                    .UseBlob(context =>
                    {
                        observed = context;
                        return Task.CompletedTask;
                    })))
            .Build();

        var content = Encoding.UTF8.GetBytes("some-content");
        await app.HandleBlob("uploads/invoice-42.json", content);

        Assert.NotNull(observed);
        Assert.Equal("uploads/invoice-42.json", observed.Blob.Name);
        Assert.Equal(content, observed.Blob.Content);
    }

    [Fact]
    public async Task StringOverload_EncodesUtf8_AndDecodesBackWithGetContentAsString()
    {
        BlobTriggerEvent observed = null;

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseBlobStorage(blob => blob
                    .UseBlob(delivered =>
                    {
                        observed = delivered;
                        return Task.CompletedTask;
                    })))
            .Build();

        await app.HandleBlob("notes.txt", "héllo blob");

        Assert.NotNull(observed);
        Assert.Equal("héllo blob", observed.GetContentAsString());
    }

    [Fact]
    public async Task PipelineException_Propagates_SoTheHostRetries()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseBlobStorage(blob => blob
                    .UseBlob((BlobStorageContext _) =>
                        throw new InvalidOperationException("handler failed"))))
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.HandleBlob("bad.bin", Array.Empty<byte>()));
    }

    [Fact]
    public void PlatformNeutralOverload_NoOpsOnNonAzureBuilders()
    {
        var mockBuilder = new Mock<Benzene.Abstractions.Hosting.IBenzeneApplicationBuilder>();

        var result = mockBuilder.Object.UseBlobStorage(blob => blob
            .UseBlob((BlobStorageContext _) => Task.CompletedTask));

        Assert.Same(mockBuilder.Object, result);
    }
}
