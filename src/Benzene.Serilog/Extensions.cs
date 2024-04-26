using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Serilog;

namespace Benzene.Microsoft.Logging;

public static class Extensions
{
    public static IBenzeneServiceContainer AddSerilog(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IBenzeneLogAppender, SerilogBenzeneLogAppender>();
        return services;
    }
}