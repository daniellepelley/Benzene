using System.Collections.Generic;
using Benzene.SelfHost;
using Xunit;

namespace Benzene.Test.Hosting;

public class InlineSelfHostedStartUpTest
{
    [Fact]
    public void Build_RunsConfigureServicesBeforeConfigure()
    {
        // ConfigureServices must run before Configure, matching every other host. Otherwise a
        // service the caller registers in ConfigureServices (e.g. a custom ISerializer before
        // AddBenzene) would lose the TryAdd race to whatever the Configure path registers first.
        var order = new List<string>();

        new InlineSelfHostedStartUp()
            .ConfigureServices(_ => order.Add("services"))
            .Configure(_ => order.Add("configure"))
            .Build();

        Assert.Equal(new[] { "services", "configure" }, order);
    }
}
