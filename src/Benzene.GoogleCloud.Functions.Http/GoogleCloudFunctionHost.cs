using Benzene.Abstractions.Middleware;
using Benzene.GoogleCloud.Functions.Core;
using Benzene.Microsoft.Dependencies;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;

namespace Benzene.GoogleCloud.Functions.Http;

/// <summary>
/// Hosts a platform-neutral <see cref="BenzeneStartUp"/> as a Google Cloud Functions Gen2 HTTP
/// trigger. Subclass with your StartUp (<c>public class Function : GoogleCloudFunctionHost&lt;Startup&gt;
/// { }</c>) and point <c>gcloud functions deploy --entry-point</c> at the resulting class -
/// mirrors <c>Benzene.Aws.Lambda.Core.AwsLambdaHost&lt;TStartUp&gt;</c>'s exact shape.
/// </summary>
/// <remarks>
/// The same <c>Startup</c> class works unchanged on Cloud Run (via
/// <c>Benzene.AspNet.Core</c>'s existing <c>WebApplicationBuilder.UseBenzene&lt;TStartUp&gt;()</c>)
/// - see <see cref="GoogleCloudFunctionApplicationBuilder"/>'s remarks for why.
/// </remarks>
/// <typeparam name="TStartUp">The platform-neutral application definition to host.</typeparam>
public class GoogleCloudFunctionHost<TStartUp> : IHttpFunction where TStartUp : BenzeneStartUp, new()
{
    private readonly IEntryPointMiddlewareApplication<HttpContext> _app;

    /// <summary>
    /// Constructs <typeparamref name="TStartUp"/>, runs its configuration/service registration, and
    /// builds the entry point application every invocation dispatches through.
    /// </summary>
    public GoogleCloudFunctionHost()
    {
        var (startUp, configuration, services, container) = GoogleCloudStartUpRunner.Bootstrap<TStartUp>();
        var appBuilder = new GoogleCloudFunctionApplicationBuilder(container);

        startUp.ConfigureServices(services, configuration);
        startUp.Configure(appBuilder, configuration);

        _app = appBuilder.Build(new MicrosoftServiceResolverFactory(services));
    }

    /// <summary>
    /// Handles an incoming HTTP request - the Cloud Functions Framework entry point every invocation
    /// dispatches through.
    /// </summary>
    /// <param name="context">The incoming HTTP context.</param>
    public Task HandleAsync(HttpContext context) => _app.SendAsync(context);
}
