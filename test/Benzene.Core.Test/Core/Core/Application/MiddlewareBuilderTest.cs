using Benzene.Abstractions.Info;
using Benzene.Core.DI;
using Benzene.Core.Info;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.Application;

public class ApplicationTest
{
    [Fact]
    public void BlankApplicationInfoIsRegistered()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene());

        using var factory = new MicrosoftServiceResolverFactory(services);

        using var serviceResolver = factory.CreateScope();

        var applicationInfo = serviceResolver.GetService<IApplicationInfo>();
        Assert.IsType<BlankApplicationInfo>(applicationInfo);
    }

    [Fact]
    public void ApplicationInfoIsRegistered()
    {
        var services = new ServiceCollection();
        var name = "some-name";
        var version = "some-version";
        var description = "some-description";
        services.UsingBenzene(x => x
            .SetApplicationInfo(name, version, description));

        using var factory = new MicrosoftServiceResolverFactory(services);

        using var serviceResolver = factory.CreateScope();

        var applicationInfo = serviceResolver.GetService<IApplicationInfo>();
        Assert.IsType<ApplicationInfo>(applicationInfo);

        Assert.Equal(name, applicationInfo.Name);
        Assert.Equal(version, applicationInfo.Version);
        Assert.Equal(description, applicationInfo.Description);
    }
}
