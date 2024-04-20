using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;

namespace Benzene.Examples.App.Logging
{
    public static class Extensions
    {
        public static IBenzeneServiceContainer AddStructuredLogging(this IBenzeneServiceContainer services)
        {
            return services
                .AddScoped<ILogContext, SerilogLogContext>();
        }
    }
}
