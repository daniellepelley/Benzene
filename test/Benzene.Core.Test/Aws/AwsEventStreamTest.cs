﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws;

public class AwsEventStreamTest
{
    [Fact]
    public async Task SendEventStream()
    {
        const string message = "Test";

        var aws = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        aws
            .Use(new FuncWrapperMiddleware<AwsEventStreamContext>((_, next) => next()))
            .Use(null, (context, next) =>
            {
                context.Response = StringToStream(message); 
                return next();
            });
        var awsEventStreamContext = new AwsEventStreamContext(StringToStream(message), Mock.Of<ILambdaContext>());

        await aws.Build().HandleAsync(awsEventStreamContext, ServiceResolverMother.CreateServiceResolver());

        var response = StreamToString(awsEventStreamContext.Response);

        Assert.Equal(message, response);
    }

    private static Stream StringToStream(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value));
    }

    private static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
