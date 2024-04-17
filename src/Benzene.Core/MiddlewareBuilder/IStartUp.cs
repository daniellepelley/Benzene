using Benzene.Abstractions.MiddlewareBuilder;
using Microsoft.Extensions.Configuration;

namespace Benzene.Core.MiddlewareBuilder;

public interface IStartUp<TContainer, TApp>
{
    IConfiguration GetConfiguration();
    void ConfigureServices(TContainer services, IConfiguration configuration);
    void Configure(TApp app, IConfiguration configuration);
}
