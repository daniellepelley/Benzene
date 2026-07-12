using Benzene.Clients.Aws.Sqs;
using Xunit;

namespace Benzene.Test.Aws.Client.Sqs;

public class LocalSqsClientFactoryTest
{
    [Fact]
    public void Create_UnknownProfile_ReturnsNull()
    {
        var result = LocalSqsClientFactory.Create("some-profile-that-does-not-exist");

        Assert.Null(result);
    }
}
