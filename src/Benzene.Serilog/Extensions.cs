using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;

namespace Benzene.Serilog;

public static class Extensions
{
    public static IBenzeneServiceContainer AddSerilog(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IBenzeneLogAppender, SerilogBenzeneLogAppender>();
        return services;
    }
}