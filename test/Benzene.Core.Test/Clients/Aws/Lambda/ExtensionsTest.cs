using Benzene.Clients.Aws;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Clients.Aws.Lambda;

public class ExtensionsTest
{
    [Fact]
    public void AddLambdaClients()
    {
        var services = new ServiceCollection();
        var benzeneServices = new Microsoft.Dependencies.MicrosoftBenzeneServiceContainer(services);

        benzeneServices.AddLambdaClients("some-sender");

        Assert.NotEmpty(services);
    }
}
