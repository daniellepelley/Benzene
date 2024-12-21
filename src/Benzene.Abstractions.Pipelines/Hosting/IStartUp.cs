namespace Benzene.Abstractions.Hosting;

public interface IStartUp<TContainer, TConfiguration, TAppBuilder>
{
    TConfiguration GetConfiguration();
    void ConfigureServices(TContainer services, TConfiguration configuration);
    void Configure(TAppBuilder app, TConfiguration configuration);
}
