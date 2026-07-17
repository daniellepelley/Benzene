using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.CosmosDb;
using Benzene.Core.Exceptions;
using Benzene.Core.Middleware;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class CosmosDbChangeFeedPipelineTest
{
    private class OrderDocument
    {
        public string Id { get; set; }
        public decimal Total { get; set; }
    }

    private class CustomerDocument
    {
        public string Id { get; set; }
    }

    [Fact]
    public async Task ChangeFeedBatch_IsDeliveredAsOneOrderedStream_InASingleRun()
    {
        var collected = new List<string>();
        var runs = 0;

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseCosmosDbChangeFeed<OrderDocument>(feed => feed
                    .UseStream<OrderDocument>(async (documents, _) =>
                    {
                        runs++;
                        await foreach (var document in documents)
                        {
                            collected.Add(document.Id);
                        }
                    })))
            .Build();

        await app.HandleCosmosDbChanges<OrderDocument>(new[]
        {
            new OrderDocument { Id = "order-1" },
            new OrderDocument { Id = "order-2" },
            new OrderDocument { Id = "order-3" }
        });

        Assert.Equal(1, runs);
        Assert.Equal(new[] { "order-1", "order-2", "order-3" }, collected);
    }

    [Fact]
    public async Task EmptyBatch_RunsThePipelineOnce_WithNoItems()
    {
        var collected = new List<OrderDocument>();
        var runs = 0;

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseCosmosDbChangeFeed<OrderDocument>(feed => feed
                    .UseStream<OrderDocument>(async (documents, _) =>
                    {
                        runs++;
                        await foreach (var document in documents)
                        {
                            collected.Add(document);
                        }
                    })))
            .Build();

        await app.HandleCosmosDbChanges(Array.Empty<OrderDocument>());

        Assert.Equal(1, runs);
        Assert.Empty(collected);
    }

    [Fact]
    public async Task NullBatch_IsTreatedAsEmpty()
    {
        var runs = 0;

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseCosmosDbChangeFeed<OrderDocument>(feed => feed
                    .UseStream<OrderDocument>(async (documents, _) =>
                    {
                        runs++;
                        await foreach (var unused in documents) { }
                    })))
            .Build();

        await app.HandleCosmosDbChanges<OrderDocument>(null);

        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task TwoDocumentTypes_RouteToTheirOwnPipelines()
    {
        var orderIds = new List<string>();
        var customerIds = new List<string>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app =>
            {
                app.UseCosmosDbChangeFeed<OrderDocument>(feed => feed
                    .UseStream<OrderDocument>(async (documents, _) =>
                    {
                        await foreach (var document in documents)
                        {
                            orderIds.Add(document.Id);
                        }
                    }));
                app.UseCosmosDbChangeFeed<CustomerDocument>(feed => feed
                    .UseStream<CustomerDocument>(async (documents, _) =>
                    {
                        await foreach (var document in documents)
                        {
                            customerIds.Add(document.Id);
                        }
                    }));
            })
            .Build();

        await app.HandleCosmosDbChanges<OrderDocument>(new[] { new OrderDocument { Id = "order-1" } });
        await app.HandleCosmosDbChanges<CustomerDocument>(new[] { new CustomerDocument { Id = "customer-1" } });

        Assert.Equal(new[] { "order-1" }, orderIds);
        Assert.Equal(new[] { "customer-1" }, customerIds);
    }

    [Fact]
    public async Task HandlerException_PropagatesToTheCaller_SoTheTriggerRetainsItsLease()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseCosmosDbChangeFeed<OrderDocument>(feed => feed
                    .UseStream<OrderDocument>((_, _) => throw new InvalidOperationException("handler failed"))))
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.HandleCosmosDbChanges<OrderDocument>(new[] { new OrderDocument { Id = "order-1" } }));
    }

    [Fact]
    public async Task UnregisteredDocumentType_Throws()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseCosmosDbChangeFeed<OrderDocument>(feed => feed
                    .UseStream<OrderDocument>(async (documents, _) =>
                    {
                        await foreach (var unused in documents) { }
                    })))
            .Build();

        await Assert.ThrowsAsync<BenzeneException>(() =>
            app.HandleCosmosDbChanges<CustomerDocument>(new[] { new CustomerDocument { Id = "customer-1" } }));
    }

    [Fact]
    public void PlatformNeutralOverload_NoOpsOnNonAzureBuilders()
    {
        var mockBuilder = new Mock<Benzene.Abstractions.Hosting.IBenzeneApplicationBuilder>();

        var result = mockBuilder.Object.UseCosmosDbChangeFeed<OrderDocument>(feed => feed
            .UseStream<OrderDocument>(async (documents, _) =>
            {
                await foreach (var unused in documents) { }
            }));

        Assert.Same(mockBuilder.Object, result);
    }
}
