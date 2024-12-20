using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Core.Info;

namespace Benzene.Aws.EventBridge;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddS3(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("s3"));
        
        return services;
    }
}


