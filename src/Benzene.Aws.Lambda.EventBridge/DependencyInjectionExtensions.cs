﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Info;

namespace Benzene.Aws.EventBridge;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddS3(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("s3"));
        
        return services;
    }
}


