using Benzene.Abstractions.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Testing;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;

namespace Benzene.GoogleCloud.Functions.Http.TestHelpers;

/// <summary>
/// Provides the Google Cloud Functions bridge for <see cref="BenzeneTestHostBuilder{TStartUp}"/>.
/// </summary>
public static class BenzeneTestHostExtensions
{
    /// <summary>
    /// Builds an <see cref="IHttpFunction"/> from the StartUp, configured services, and any
    /// overrides registered on <paramref name="builder"/> — the same construction
    /// <see cref="GoogleCloudFunctionHost{TStartUp}"/> performs for a real deployment, with a seam
    /// for test overrides. Dispatch into it with <see cref="SendHttpAsync"/>.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <param name="builder">The test host builder, with any <c>WithServices</c>/<c>WithConfiguration</c> overrides already applied.</param>
    /// <returns>The built Cloud Function.</returns>
    public static IHttpFunction BuildGoogleCloudFunctionHost<TStartUp>(this BenzeneTestHostBuilder<TStartUp> builder)
        where TStartUp : BenzeneStartUp, new()
    {
        return builder.Build((startUp, services, configuration) =>
        {
            var container = new MicrosoftBenzeneServiceContainer(services);
            var appBuilder = new GoogleCloudFunctionApplicationBuilder(container);

            startUp.Configure(appBuilder, configuration);

            var app = appBuilder.Build(new MicrosoftServiceResolverFactory(services));

            return new TestGoogleCloudFunction(app);
        });
    }

    /// <summary>
    /// Sends <paramref name="context"/> through <paramref name="function"/> and returns it, populated
    /// with whatever response the pipeline wrote.
    /// </summary>
    /// <param name="function">The Cloud Function to dispatch into (typically built by <see cref="BuildGoogleCloudFunctionHost{TStartUp}"/>).</param>
    /// <param name="context">The HTTP context to send (typically built by <see cref="HttpContextBuilder"/>).</param>
    /// <returns>The same <paramref name="context"/>, once <paramref name="function"/> has finished handling it.</returns>
    public static async Task<HttpContext> SendHttpAsync(this IHttpFunction function, HttpContext context)
    {
        await function.HandleAsync(context);
        return context;
    }

    private sealed class TestGoogleCloudFunction : IHttpFunction
    {
        private readonly IEntryPointMiddlewareApplication<HttpContext> _app;

        public TestGoogleCloudFunction(IEntryPointMiddlewareApplication<HttpContext> app)
        {
            _app = app;
        }

        public Task HandleAsync(HttpContext context) => _app.SendAsync(context);
    }
}
