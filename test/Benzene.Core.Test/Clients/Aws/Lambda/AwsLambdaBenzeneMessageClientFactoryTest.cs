using Amazon.Lambda;
using Benzene.Clients.Aws.Lambda;
using Benzene.Test.Examples;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Clients.Aws.Lambda;

public class AwsLambdaBenzeneMessageClientFactoryTest
{
    [Fact]
    public void CreatesClient()
    {
        var factory = new AwsLambdaBenzeneMessageClientFactory(Defaults.LambdaName, Mock.Of<IAmazonLambda>(), NullLogger<AwsLambdaBenzeneMessageClient>.Instance);
        var client = factory.Create();
        Assert.NotNull(client);
    }
}
