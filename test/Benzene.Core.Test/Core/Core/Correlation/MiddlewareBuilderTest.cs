﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Diagnostics.Correlation;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.Correlation;

public class CorrelationMiddlewareTest
{
    [Fact]
    public async Task AddsNewCorrelationId()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage());

        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        var items = ((MiddlewarePipelineBuilder<BenzeneMessageContext>)middlewarePipelineBuilder
            .UseCorrelationId("foo"))
            .GetItems();

        Assert.Single(items);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await middlewarePipelineBuilder.Build().HandleAsync(context, serviceResolver);

        var correlationId = serviceResolver.GetService<ICorrelationId>();

        Assert.NotNull(correlationId.Get());
    }

    [Fact]
    public async Task AddsExistingCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString();

        var services = new ServiceCollection();  
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage());

        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        var items = ((MiddlewarePipelineBuilder<BenzeneMessageContext>)middlewarePipelineBuilder
            .UseCorrelationId("foo"))
            .GetItems();

        Assert.Single(items);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest
        {
            Headers = new Dictionary<string, string>
            {
                {"foo", correlationId }
            }
        });
        await middlewarePipelineBuilder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal(correlationId, serviceResolver.GetService<ICorrelationId>().Get());
    }

}
