using Benzene.Abstractions.Hosting;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.Core;

/// <summary>
/// Provides extension methods for configuring Azure Functions-specific settings on a platform-neutral
/// <see cref="IBenzeneApplicationBuilder"/>.
/// </summary>
public static class AzureFunctionAppBuilderExtensions
{
    /// <summary>
    /// Registers the services required to resolve <see cref="IBenzeneInvocation"/> on Azure Functions.
    /// </summary>
    /// <remarks>
    /// Unlike AWS Lambda and ASP.NET Core, Azure Functions has no single request-flowing pipeline this
    /// registration can also populate the invocation from -- the isolated worker dispatches each trigger
    /// type through its own separate pipeline. Population instead happens in the worker middleware
    /// (<see cref="FunctionsWorkerApplicationBuilderExtensions.UseBenzene"/>), which every host wires up
    /// in <c>Program.cs</c>. Call this from <c>BenzeneStartUp.Configure</c> to opt in.
    /// </remarks>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseBenzeneInvocation(this IBenzeneApplicationBuilder app)
    {
        app.Register(x => x.AddBenzeneInvocation());
        return app;
    }
}
