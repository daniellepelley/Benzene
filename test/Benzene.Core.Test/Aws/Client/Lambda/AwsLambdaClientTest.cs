using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Clients.Aws.Lambda;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.Lambda;

public class AwsLambdaClientTest
{
    private static MemoryStream ToPayloadStream(string json)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    [Fact]
    public async Task SendMessageAsync_EventInvocation_ReturnsDefault()
    {
        var mockLambdaClient = new Mock<IAmazonLambda>();
        mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), default))
            .ReturnsAsync(new InvokeResponse());

        var client = new AwsLambdaClient(mockLambdaClient.Object);

        var result = await client.SendMessageAsync<string, string>("some-request", "some-function", InvocationType.Event);

        Assert.Null(result);
        mockLambdaClient.Verify(x => x.InvokeAsync(
            It.Is<InvokeRequest>(r => r.InvocationType == InvocationType.Event && r.FunctionName == "some-function"), default));
    }

    [Fact]
    public async Task SendMessageAsync_RequestResponseInvocation_DeserializesPayload()
    {
        var mockLambdaClient = new Mock<IAmazonLambda>();
        mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), default))
            .ReturnsAsync(new InvokeResponse
            {
                Payload = ToPayloadStream("\"some-response\"")
            });

        var client = new AwsLambdaClient(mockLambdaClient.Object);

        var result = await client.SendMessageAsync<string, string>("some-request", "some-function", InvocationType.RequestResponse);

        Assert.Equal("some-response", result);
    }

    [Fact]
    public async Task SendMessageAsync_FunctionErrorSet_ThrowsInsteadOfMisDeserializingTheErrorPayload()
    {
        // A RequestResponse invoke where the function threw returns HTTP 200 with FunctionError set and
        // an error object as the payload - not the normal output. It must not be deserialized as TResponse.
        var mockLambdaClient = new Mock<IAmazonLambda>();
        mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), default))
            .ReturnsAsync(new InvokeResponse
            {
                FunctionError = "Unhandled",
                Payload = ToPayloadStream("{\"errorType\":\"NullReferenceException\",\"errorMessage\":\"boom\"}")
            });

        var client = new AwsLambdaClient(mockLambdaClient.Object);

        var exception = await Assert.ThrowsAsync<AwsLambdaFunctionErrorException>(
            () => client.SendMessageAsync<string, string>("some-request", "some-function", InvocationType.RequestResponse));

        Assert.Equal("some-function", exception.FunctionName);
        Assert.Equal("Unhandled", exception.FunctionError);
        Assert.Contains("NullReferenceException", exception.ErrorPayload);
    }
}
