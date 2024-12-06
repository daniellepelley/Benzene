using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Tools.Aws;

public class AwsLambdaBenzeneTestStartUp<TStartUp> where TStartUp : IStartUp<IServiceCollection, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>
{
    private readonly List<Action<IServiceCollection>> _actions = new();
    private readonly Dictionary<string, string> _dictionary = new();

    public AwsLambdaBenzeneTestStartUp<TStartUp> WithConfiguration(Dictionary<string, string> dictionary)
    {
        foreach (var keyPairValue in dictionary)        
        {
            _dictionary.Add(keyPairValue.Key, keyPairValue.Value);
        }
        return this;
    }

    public AwsLambdaBenzeneTestStartUp<TStartUp> WithConfiguration(string key, string value)
    {
        _dictionary.Add(key, value);
        return this;
    }

    public AwsLambdaBenzeneTestStartUp<TStartUp> WithServices(Action<IServiceCollection> action)
    {
        _actions.Add(action);
        return this;
    }

    public IAwsLambdaEntryPoint Build()
    {
        SetEnvironmentVariables(_dictionary);
        var startup = Activator.CreateInstance<TStartUp>();

        var configuration = startup.GetConfiguration();
        var configurationBuilder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(_dictionary);

        var services = new ServiceCollection();
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services));

        startup.ConfigureServices(services, configurationBuilder.Build());
        foreach (var action in _actions)
        {
            action(services);
        }

        startup.Configure(app, configuration);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        return new AwsLambdaEntryPoint(app.Build(), serviceResolverFactory);
    }
    
    private static void SetEnvironmentVariables(IDictionary<string, string> dictionary)
    {
        foreach (var keyValuePair in dictionary)
        {
           Environment.SetEnvironmentVariable(keyValuePair.Key, keyValuePair.Value); 
        }
    }
}
