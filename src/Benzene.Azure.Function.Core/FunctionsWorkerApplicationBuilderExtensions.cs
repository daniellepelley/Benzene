using Benzene.Abstractions.Hosting;
using Benzene.Core.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Benzene.Azure.Function.Core;

/// <summary>
/// Provides the Azure Functions isolated-worker implementation of <see cref="IBenzeneInvocation"/>.
/// </summary>
public static class FunctionsWorkerApplicationBuilderExtensions
{
    /// <summary>
    /// Registers Benzene's worker middleware, which populates an <see cref="IBenzeneInvocation"/> for the
    /// duration of each invocation (when <see cref="AzureFunctionAppBuilderExtensions.UseBenzeneInvocation"/>
    /// has been called in <c>BenzeneStartUp.Configure</c>), with
    /// <see cref="IBenzeneInvocation.InvocationId"/> set to the isolated worker's invocation ID and
    /// <c>GetFeature&lt;FunctionContext&gt;()</c> returning the native <see cref="FunctionContext"/>.
    /// Call this inside <c>ConfigureFunctionsWorkerDefaults(w =&gt; w.UseBenzene())</c> in <c>Program.cs</c>.
    /// </summary>
    /// <param name="builder">The isolated worker application builder.</param>
    /// <returns><paramref name="builder"/>, for method chaining.</returns>
    public static IFunctionsWorkerApplicationBuilder UseBenzene(this IFunctionsWorkerApplicationBuilder builder)
    {
        return builder.UseMiddleware(async (context, next) =>
        {
            var accessor = context.InstanceServices.GetService<IBenzeneInvocationAccessor>();
            if (accessor != null)
            {
                accessor.Invocation = new BenzeneInvocation(context.InvocationId, AzureFunctionAppBuilder.PlatformName,
                    new Dictionary<Type, object> { [typeof(FunctionContext)] = context });
            }

            await next();
        });
    }
}
