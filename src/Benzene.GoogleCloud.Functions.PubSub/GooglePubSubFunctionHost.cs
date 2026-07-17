using Benzene.Abstractions.Middleware;
using Benzene.GoogleCloud.Functions.Core;
using Benzene.Microsoft.Dependencies;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Events.Protobuf.Cloud.PubSub.V1;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Hosts a platform-neutral <see cref="BenzeneStartUp"/> as a Google Cloud Functions Gen2 Pub/Sub
/// CloudEvent trigger. Subclass with your StartUp (<c>public class Function :
/// GooglePubSubFunctionHost&lt;Startup&gt; { }</c>) and point <c>gcloud functions deploy
/// --entry-point</c> at the resulting class - mirrors
/// <c>Benzene.GoogleCloud.Functions.Http.GoogleCloudFunctionHost&lt;TStartUp&gt;</c>'s exact shape
/// for the Pub/Sub CloudEvent trigger type instead of HTTP.
/// </summary>
/// <typeparam name="TStartUp">The platform-neutral application definition to host.</typeparam>
public class GooglePubSubFunctionHost<TStartUp> : ICloudEventFunction<MessagePublishedData> where TStartUp : BenzeneStartUp, new()
{
    private readonly IEntryPointMiddlewareApplication<MessagePublishedData> _app;

    /// <summary>
    /// Constructs <typeparamref name="TStartUp"/>, runs its configuration/service registration, and
    /// builds the entry point application every invocation dispatches through.
    /// </summary>
    public GooglePubSubFunctionHost()
    {
        var (startUp, configuration, services, container) = GoogleCloudStartUpRunner.Bootstrap<TStartUp>();
        var appBuilder = new GooglePubSubFunctionApplicationBuilder(container);

        startUp.ConfigureServices(services, configuration);
        startUp.Configure(appBuilder, configuration);

        _app = appBuilder.Build(new MicrosoftServiceResolverFactory(services));
    }

    /// <summary>
    /// Handles an incoming Pub/Sub message - the Cloud Functions Framework entry point every
    /// invocation dispatches through.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent envelope. Unused - Pub/Sub-specific data already lives on <paramref name="data"/>.</param>
    /// <param name="data">The Pub/Sub message payload for this invocation.</param>
    /// <param name="cancellationToken">Unused - the middleware pipeline does not accept a cancellation token today, matching every other Benzene transport adapter.</param>
    public Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken) => _app.SendAsync(data);
}
