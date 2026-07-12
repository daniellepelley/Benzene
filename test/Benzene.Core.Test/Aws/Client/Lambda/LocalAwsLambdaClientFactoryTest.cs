using Benzene.Clients.Aws.Lambda;
using Xunit;

namespace Benzene.Test.Aws.Client.Lambda;

public class LocalAwsLambdaClientFactoryTest
{
    [Fact]
    public void Create_UnknownProfile_ReturnsNull()
    {
        var result = LocalAwsLambdaClientFactory.Create("some-profile-that-does-not-exist");

        Assert.Null(result);
    }
}
