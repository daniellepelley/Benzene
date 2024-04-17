using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.DI;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Tools;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Extensions = Benzene.Microsoft.Dependencies.Extensions;

namespace Benzene.Test.Aws;

public class ApiGatewayCustomAuthorizerMessagePipelineTest
{
    private static APIGatewayCustomAuthorizerRequest CreateRequest()
    {
        return HttpBuilder.Create("GET", "/example", new { value = "some-message" }).AsApiGatewayCustomAuthorizerEvent();
    }

    [Fact]
    public async Task Send()
    {
        var services = new ServiceCollection();
        Extensions.UsingBenzene(services, x => x.AddBenzene());

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var pipeline = new MiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext>(new MicrosoftBenzeneServiceContainer(services))
            .Use(null, (context, next) =>
        {
            context.ApiGatewayCustomAuthorizerResponse = new APIGatewayCustomAuthorizerResponse
            {
                PrincipalID = "some-id"
            };
            return next();
        });

        var aws = new ApiGatewayCustomAuthorizerApplication(pipeline.AsPipeline());

        var request = CreateRequest();

        var response = await aws.HandleAsync(request, serviceResolver);

        Assert.NotNull(response);
        Assert.Equal("some-id", response.PrincipalID);
    }

    [Fact]
    public async Task Send_FromStream()
    {
        ApiGatewayCustomAuthorizerContext apiGatewayCustomAuthorizerContext = null;
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        app.UseApiGatewayCustomAuthorizer(message => message
            .Use(null, (context, next) =>
            {
                apiGatewayCustomAuthorizerContext = context;
                context.ApiGatewayCustomAuthorizerResponse = new APIGatewayCustomAuthorizerResponse
                {
                    PolicyDocument = new APIGatewayCustomAuthorizerPolicy
                    {
                        Version = "some-version"
                    },
                    PrincipalID = "some-id"
                };
                return next();
            })
        );

        var request = CreateRequest();

        await app.AsPipeline().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.Equal("some-version", apiGatewayCustomAuthorizerContext.ApiGatewayCustomAuthorizerResponse.PolicyDocument.Version);
        Assert.Equal("some-id", apiGatewayCustomAuthorizerContext.ApiGatewayCustomAuthorizerResponse.PrincipalID);
    }
}
