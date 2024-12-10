using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Benzene.Serilog.Logging
{
    public static class Extensions
    {
        public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseSerilog(
            this IMiddlewarePipelineBuilder<AwsEventStreamContext> app)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(new CustomJsonFormatter())
                .CreateLogger();

            return app.Use(resolver => new FuncWrapperMiddleware<AwsEventStreamContext>("LogRequestId", async (context, next) =>
            {
                LogContext.PushProperty("requestId", context.LambdaContext.AwsRequestId);
                await next();
            }));
        }
    }
}
