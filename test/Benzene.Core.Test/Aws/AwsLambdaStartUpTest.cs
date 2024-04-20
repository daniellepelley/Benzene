using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Benzene.Core.DirectMessage;
using Benzene.Results;
using Benzene.Test.Aws.Examples;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Tools.Aws;
using Xunit;

namespace Benzene.Test.Aws;

public class AwsLambdaStartUpTest
{
    [Fact]
    public async Task LambdaEntryPoint()
    {
        using var demoAwsStartUp = new DemoAwsLambdaStartUp();

        var request = RequestMother.CreateExampleEvent().AsDirectMessage();
        var response = await demoAwsStartUp.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
        var directMessageResponse = AwsLambdaBenzeneTestHost.StreamToObject<DirectMessageResponse>(response);
        Assert.Equal(ServiceResultStatus.Ok, directMessageResponse.StatusCode);
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

        var request = RequestMother.CreateExampleEvent().AsDirectMessage();
        var directMessageResponse = await testLambdaHosting.SendEventAsync<DirectMessageResponse>(request);
        
        Assert.Equal(ServiceResultStatus.Ok, directMessageResponse.StatusCode);
    }
}
