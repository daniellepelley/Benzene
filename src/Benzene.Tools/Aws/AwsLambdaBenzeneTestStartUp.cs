using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Tools.Aws;

public class AwsLambdaBenzeneTestStartUp<TStartUp>
    : AwsLambdaBenzeneTestStartUp<TStartUp, IServiceCollection> where TStartUp : IStartUp<IServiceCollection, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>
{
    public AwsLambdaBenzeneTestStartUp()
     : base(new MicrosoftDependencyInjectionAdapter())
    {
        
    } 
}

public class AwsLambdaBenzeneTestStartUp<TStartUp, TContainer> where TStartUp : IStartUp<TContainer, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>
{
    private readonly List<Action<TContainer>> _actions = new();
    private readonly Dictionary<string, string> _dictionary = new();
    private readonly IDependencyInjectionAdapter<TContainer> _dependencyInjectionAdapter;

    public AwsLambdaBenzeneTestStartUp(IDependencyInjectionAdapter<TContainer> dependencyInjectionAdapter)
    {
        _dependencyInjectionAdapter = dependencyInjectionAdapter;
    }
    
    public AwsLambdaBenzeneTestStartUp<TStartUp, TContainer> WithConfiguration(Dictionary<string, string> dictionary)
    {
        foreach (var keyPairValue in dictionary)        
        {
            _dictionary.Add(keyPairValue.Key, keyPairValue.Value);
        }
        return this;
    }

    public AwsLambdaBenzeneTestStartUp<TStartUp, TContainer> WithConfiguration(string key, string value)
    {
        _dictionary.Add(key, value);
        return this;
    }

    public  AwsLambdaBenzeneTestStartUp<TStartUp, TContainer> WithServices(Action<TContainer> action)
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

        var services = _dependencyInjectionAdapter.CreateContainer();
        var app = new AwsEventStreamPipelineBuilder(_dependencyInjectionAdapter.CreateBenzeneServiceContainer(services));

        startup.ConfigureServices(services, configurationBuilder.Build());
        foreach (var action in _actions)
        {
            action(services);
        }

        startup.Configure(app, configuration);

        return new AwsLambdaEntryPoint(app.Build(), _dependencyInjectionAdapter.CreateBenzeneServiceResolverFactory(services));
    }
    
    private static void SetEnvironmentVariables(IDictionary<string, string> dictionary)
    {
        foreach (var keyValuePair in dictionary)
        {
           Environment.SetEnvironmentVariable(keyValuePair.Key, keyValuePair.Value); 
        }
    }
}
