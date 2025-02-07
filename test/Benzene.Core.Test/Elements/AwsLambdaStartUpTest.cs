using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Core.MessageHandlers.BenzeneMessage.TestHelpers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Elements.Examples;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Xunit;

namespace Benzene.Test.Elements;

public class AwsLambdaStartUpTest
{
    [Fact]
    public async Task LambdaEntryPoint_Direct()
    {
        var demoAwsStartUp = new LambdaEntryPoint();

        var request = RequestMother.CreateExampleEvent().AsBenzeneMessage();
        var response = await demoAwsStartUp.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
        var directMessageResponse = AwsLambdaBenzeneTestHost.StreamToObject<BenzeneMessageResponse>(response);
        Assert.Equal("Ok", directMessageResponse.StatusCode);
    }

    // [Fact]
    // public async Task LambdaEntryPoint_Sns()
    // {
    //     var demoAwsStartUp = new LambdaEntryPoint();
    //
    //     var request = RequestMother.CreateExampleEvent().AsSns();
    //     var response = await demoAwsStartUp.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
    //     Assert.NotNull(response);
    // }
    //
    // [Fact]
    // public async Task LambdaEntryPoint_Sqs()
    // {
    //     var demoAwsStartUp = new LambdaEntryPoint();
    //
    //     var request = RequestMother.CreateExampleEvent().AsSqs();
    //     var response = await demoAwsStartUp.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
    //     Assert.NotNull(response);
    // }

    [Fact]
    public async Task LambdaEntryPoint_ApiGateway()
    {
        var demoAwsStartUp = new LambdaEntryPoint();

        var request = RequestMother
            .CreateExampleHttp()
            .AsApiGatewayRequest();

        var response = await demoAwsStartUp.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
        var directMessageResponse = AwsLambdaBenzeneTestHost.StreamToObject<APIGatewayProxyResponse>(response);
        Assert.Equal(200, directMessageResponse.StatusCode);
    }

    [Fact]
    public async Task LambdaEntryPoint_ApiGateway_HealthCheck()
    {
        var demoAwsStartUp = new LambdaEntryPoint();

        var request = HttpBuilder.Create("POST", "/healthcheck").AsApiGatewayRequest();

        var response = await demoAwsStartUp.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
        var directMessageResponse = AwsLambdaBenzeneTestHost.StreamToObject<APIGatewayProxyResponse>(response);
        Assert.Equal(200, directMessageResponse.StatusCode);
    }


}
