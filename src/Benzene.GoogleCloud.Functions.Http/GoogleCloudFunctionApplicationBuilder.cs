using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.AspNet.Core;
using Benzene.Core.Middleware;
using Microsoft.AspNetCore.Http;

namespace Benzene.GoogleCloud.Functions.Http;

/// <summary>
/// The <see cref="IBenzeneApplicationBuilder"/>/<see cref="IAspApplicationBuilder"/> implementation
/// a <see cref="Microsoft.Dependencies.BenzeneStartUp"/> is configured against when hosted on Google
/// Cloud Functions via <see cref="GoogleCloudFunctionHost{TStartUp}"/>.
/// </summary>
/// <remarks>
/// This exists purely so <c>Benzene.AspNet.Core</c>'s existing <c>UseHttp(IBenzeneApplicationBuilder,
/// ...)</c> extension - which is a no-op unless its argument is an <see cref="IAspApplicationBuilder"/>
/// - treats a Google Cloud Functions host the same as a real ASP.NET Core one, without needing a live
/// <c>Microsoft.AspNetCore.Builder.IApplicationBuilder</c> (which Cloud Functions Framework's
/// <c>IHttpFunction</c> contract never gives us): the same <c>Startup : BenzeneStartUp</c> class that
/// calls <c>app.UseHttp(...)</c> for a Cloud Run deployment works completely unchanged here too.
/// Unlike <c>Benzene.AspNet.Core.AspApplicationBuilder</c>'s <c>Add(...)</c> (which builds the entry
/// point application immediately, since it already has a resolver factory on hand from the live ASP.NET
/// Core host), this implementation defers - <see cref="Add"/> just stores the factory, and
/// <see cref="Build"/> invokes it once <see cref="GoogleCloudFunctionHost{TStartUp}"/> has the final,
/// fully-configured <see cref="IServiceResolverFactory"/>.
/// </remarks>
public class GoogleCloudFunctionApplicationBuilder : BenzeneApplicationBuilder, IAspApplicationBuilder
{
    /// <summary>The platform identifier reported by <see cref="Benzene.Abstractions.Hosting.IBenzeneApplicationBuilder.Platform"/>.</summary>
    public const string PlatformName = "GoogleCloudFunctions";

    private Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<HttpContext>>? _appFactory;

    /// <summary>Initializes a new instance of the <see cref="GoogleCloudFunctionApplicationBuilder"/> class.</summary>
    /// <param name="benzeneServiceContainer">The service container backing this builder.</param>
    public GoogleCloudFunctionApplicationBuilder(IBenzeneServiceContainer benzeneServiceContainer)
        : base(PlatformName, benzeneServiceContainer)
    {
    }

    /// <summary>
    /// Stores the entry point application factory registered by <c>UseHttp(...)</c>, deferring
    /// construction until <see cref="Build"/> is called with the final <see cref="IServiceResolverFactory"/>.
    /// </summary>
    /// <param name="func">A factory that creates the entry point application given the current invocation's service resolver factory.</param>
    public void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<HttpContext>> func)
    {
        _appFactory = func;
    }

    /// <summary>
    /// Invokes the factory stored by <see cref="Add"/> to build the entry point application that
    /// handles every incoming <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="serviceResolverFactory">The fully-configured service resolver factory to build the application with.</param>
    /// <returns>The built entry point application.</returns>
    /// <exception cref="InvalidOperationException">
    /// No HTTP pipeline was configured - the <c>BenzeneStartUp.Configure</c> implementation must call
    /// <c>app.UseHttp(...)</c>.
    /// </exception>
    public IEntryPointMiddlewareApplication<HttpContext> Build(IServiceResolverFactory serviceResolverFactory)
    {
        if (_appFactory == null)
        {
            throw new InvalidOperationException(
                "No HTTP pipeline configured - call app.UseHttp(...) in your BenzeneStartUp.Configure implementation.");
        }

        return _appFactory(serviceResolverFactory);
    }
}
