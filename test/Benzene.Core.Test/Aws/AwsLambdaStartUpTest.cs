using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Autofac;
using Benzene.Autofac;
using Benzene.Core.MessageHandlers.BenzeneMessage.TestHelpers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;
using Benzene.Test.Aws.Examples;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Tools.Aws;
using Xunit;

namespace Benzene.Test.Aws;

public class AwsLambdaStartUpTest
{
    [Fact]
    public async Task LambdaEntryPoint()
    {
        using var demoAwsStartUp = new DemoAwsLambdaStartUp();

        var request = RequestMother.CreateExampleEvent().AsBenzeneMessage();
        var response = await demoAwsStartUp.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
        var directMessageResponse = AwsLambdaBenzeneTestHost.StreamToObject<BenzeneMessageResponse>(response);
        Assert.Equal(BenzeneResultStatus.Ok, directMessageResponse.StatusCode);
    }

    [Fact]
    public async Task LambdaEntryPoint_Autofac()
    {
        using var demoAwsStartUp = new DemoAutofacAwsLambdaStartUp();

        var request = RequestMother.CreateExampleEvent().AsBenzeneMessage();
        var response = await demoAwsStartUp.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
        var directMessageResponse = AwsLambdaBenzeneTestHost.StreamToObject<BenzeneMessageResponse>(response);
        Assert.Equal(BenzeneResultStatus.Ok, directMessageResponse.StatusCode);
    }

    [Fact]
    public async Task LambdaEntryPoint_WithTestHosting()
    {
        using var testLambdaHosting = new AwsLambdaBenzeneTestStartUp<DemoAwsLambdaStartUp>()
            .WithConfiguration(new Dictionary<string, string>
            {
                { "Key1", "Value1"},
                { "Key2", "Value2"}
            })
            .BuildHost();

        var request = RequestMother.CreateExampleEvent().AsBenzeneMessage();
        var directMessageResponse = await testLambdaHosting.SendEventAsync<BenzeneMessageResponse>(request);
        
        Assert.Equal(BenzeneResultStatus.Ok, directMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task LambdaEntryPoint_WithTestHostingAutofac()
    {
        using var testLambdaHosting = new AwsLambdaBenzeneTestStartUp<DemoAutofacAwsLambdaStartUp, ContainerBuilder>(new AutofacDependencyInjectionAdapter())
            .WithConfiguration(new Dictionary<string, string>
            {
                { "Key1", "Value1"},
                { "Key2", "Value2"}
            })
            .BuildHost();

        var request = RequestMother.CreateExampleEvent().AsBenzeneMessage();
        var directMessageResponse = await testLambdaHosting.SendEventAsync<BenzeneMessageResponse>(request);
        
        Assert.Equal(BenzeneResultStatus.Ok, directMessageResponse.StatusCode);
    }

}
