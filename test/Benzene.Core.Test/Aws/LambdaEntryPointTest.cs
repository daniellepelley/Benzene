﻿using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Benzene.Abstractions.DI;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws;

public class LambdaEntryPointTest
{
    [Fact]
    public async Task LambdaEntryPoint()
    {
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(new ServiceCollection()))
            .Use(null, async (x, next) =>
            {
                x.Response = new MemoryStream();
                await next();
            });

        var mockServiceResolverFactory = new Mock<IServiceResolverFactory>();

        mockServiceResolverFactory.Setup(x => x.CreateScope())
            .Returns(ServiceResolverMother.CreateServiceResolver());

        var lambdaEntryPoint = new AwsLambdaEntryPoint(app.Build(), mockServiceResolverFactory.Object);

        var request = new BenzeneMessageRequest();

        var result = await lambdaEntryPoint.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
        Assert.NotNull(result);
    }
}
