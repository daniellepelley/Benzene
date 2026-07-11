using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.MiddlewareBuilder;

public class MiddlewareMultiApplicationTest
{
    [Fact]
    public async Task HandleAsync_ProcessesAllContextsAndMapsResults()
    {
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        pipeline.Use(null, (context, next) =>
        {
            context.BenzeneMessageResponse = new BenzeneMessageResponse
            {
                Body = context.BenzeneMessageRequest.Body + "-handled"
            };
            return next();
        });

        var app = new MiddlewareMultiApplication<IReadOnlyCollection<string>, BenzeneMessageContext, string>(
            pipeline.Build(),
            bodies => bodies.Select(body => new BenzeneMessageContext(new BenzeneMessageRequest { Body = body })).ToArray(),
            context => context.BenzeneMessageResponse.Body);

        var results = await app.HandleAsync(new[] { "one", "two" }, ServiceResolverMother.CreateServiceResolverFactory());

        Assert.Equal(new[] { "one-handled", "two-handled" }, results);
    }
}
