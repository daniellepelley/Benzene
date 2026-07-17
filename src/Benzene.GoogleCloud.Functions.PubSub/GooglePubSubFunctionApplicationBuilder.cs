using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Google.Events.Protobuf.Cloud.PubSub.V1;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// The <see cref="IBenzeneApplicationBuilder"/> implementation a
/// <see cref="Microsoft.Dependencies.BenzeneStartUp"/> is configured against when hosted on Google
/// Cloud Functions via <see cref="GooglePubSubFunctionHost{TStartUp}"/>.
/// </summary>
/// <remarks>
/// Mirrors <c>Benzene.GoogleCloud.Functions.Http.GoogleCloudFunctionApplicationBuilder</c>'s
/// deferred-build shape, but is self-contained rather than reusing an existing ASP.NET Core-shaped
/// interface: unlike the HTTP trigger, a Pub/Sub CloudEvent trigger has no existing Benzene builder
/// abstraction to piggyback on, since <see cref="UsePubSub"/> (<c>DependencyInjectionExtensions</c>)
/// is the only extension that ever needs to recognize this builder type. <c>Add(...)</c> just stores
/// the entry point application factory, and <c>Build(...)</c> invokes it once
/// <see cref="GooglePubSubFunctionHost{TStartUp}"/> has the final, fully-configured
/// <see cref="IServiceResolverFactory"/>.
/// </remarks>
public class GooglePubSubFunctionApplicationBuilder : BenzeneApplicationBuilder
{
    /// <summary>The platform identifier reported by <see cref="Benzene.Abstractions.Hosting.IBenzeneApplicationBuilder.Platform"/>.</summary>
    public const string PlatformName = "GoogleCloudFunctions";

    private Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<MessagePublishedData>>? _appFactory;

    /// <summary>Initializes a new instance of the <see cref="GooglePubSubFunctionApplicationBuilder"/> class.</summary>
    /// <param name="benzeneServiceContainer">The service container backing this builder.</param>
    public GooglePubSubFunctionApplicationBuilder(IBenzeneServiceContainer benzeneServiceContainer)
        : base(PlatformName, benzeneServiceContainer)
    {
    }

    /// <summary>
    /// Stores the entry point application factory registered by <c>UsePubSub(...)</c>, deferring
    /// construction until <see cref="Build"/> is called with the final <see cref="IServiceResolverFactory"/>.
    /// </summary>
    /// <param name="func">A factory that creates the entry point application given the current invocation's service resolver factory.</param>
    public void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<MessagePublishedData>> func)
    {
        _appFactory = func;
    }

    /// <summary>
    /// Invokes the factory stored by <see cref="Add"/> to build the entry point application that
    /// handles every incoming Pub/Sub message.
    /// </summary>
    /// <param name="serviceResolverFactory">The fully-configured service resolver factory to build the application with.</param>
    /// <returns>The built entry point application.</returns>
    /// <exception cref="InvalidOperationException">
    /// No Pub/Sub pipeline was configured - the <c>BenzeneStartUp.Configure</c> implementation must
    /// call <c>app.UsePubSub(...)</c>.
    /// </exception>
    public IEntryPointMiddlewareApplication<MessagePublishedData> Build(IServiceResolverFactory serviceResolverFactory)
    {
        if (_appFactory == null)
        {
            throw new InvalidOperationException(
                "No Pub/Sub pipeline configured - call app.UsePubSub(...) in your BenzeneStartUp.Configure implementation.");
        }

        return _appFactory(serviceResolverFactory);
    }
}
