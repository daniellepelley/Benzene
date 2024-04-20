using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Benzene.Examples.Google.Tests.Helpers;

public class LambdaEntryPointTestServer
{
    private Dictionary<string, string> _dictionary;

    public LambdaEntryPointTestServer WithConfiguration(Dictionary<string, string> dictionary)
    {
        _dictionary = dictionary;
        return this;
    }

    public HttpFunction Build()
    {
        var configuration = DependenciesBuilder.GetConfiguration();
        var configurationBuilder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(_dictionary);

        var serviceResolverFactory = DependenciesBuilder.CreateServiceResolverFactory(configurationBuilder.Build());
        return new HttpFunction(serviceResolverFactory);
    }
}