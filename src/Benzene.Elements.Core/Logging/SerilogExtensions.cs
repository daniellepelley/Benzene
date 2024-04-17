using Benzene.Abstractions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Benzene.Elements.Core.Logging;

public static class SerilogExtensions
{
    public static IServiceCollection AddStructuredLogging(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CustomJsonFormatter())
            .CreateLogger();

        return services
            .AddLogging(x => x.AddSerilog())
            .AddScoped<IBenzeneLogContext, SerilogBenzeneLogContext>()
            .AddScoped(x =>
                x.GetService<ILoggerProvider>().CreateLogger("Service"));
    }
}
