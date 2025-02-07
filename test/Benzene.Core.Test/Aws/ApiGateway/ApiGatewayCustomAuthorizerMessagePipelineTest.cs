using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

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
        services.UsingBenzene(x => x.AddBenzene());

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        var pipeline = new MiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext>(new MicrosoftBenzeneServiceContainer(services))
            .Use(null, (context, next) =>
        {
            context.ApiGatewayCustomAuthorizerResponse = new APIGatewayCustomAuthorizerResponse
            {
                PrincipalID = "some-id"
            };
            return next();
        });

        var aws = new ApiGatewayCustomAuthorizerApplication(pipeline.Build());

        var request = CreateRequest();

        var response = await aws.HandleAsync(request, serviceResolverFactory);

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

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.Equal("some-version", apiGatewayCustomAuthorizerContext.ApiGatewayCustomAuthorizerResponse.PolicyDocument.Version);
        Assert.Equal("some-id", apiGatewayCustomAuthorizerContext.ApiGatewayCustomAuthorizerResponse.PrincipalID);
    }
}
