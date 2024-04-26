namespace Benzene.Abstractions.Hosting;

public interface IStartUp<TContainer, TConfiguration, TApp>
{
    TConfiguration GetConfiguration();
    void ConfigureServices(TContainer services, TConfiguration configuration);
    void Configure(TApp app, TConfiguration configuration);
}
