using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;

namespace Benzene.Log4Net;

public static class Extensions
{
    public static IBenzeneServiceContainer AddLog4Net(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IBenzeneLogAppender, Log4NetBenzeneLogAppender>();
        return services;
    }
}
