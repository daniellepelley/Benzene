using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Default implementation of <see cref="IAspApplicationBuilder"/>. Wires entry point applications
/// directly into the ASP.NET Core request pipeline via <see cref="IApplicationBuilder.Use(System.Func{RequestDelegate,RequestDelegate})"/>.
/// </summary>
public class AspApplicationBuilder : IAspApplicationBuilder
{
    private readonly List<Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>> _apps = new();
    private IApplicationBuilder _applicationBuilder;
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AspApplicationBuilder"/> class, reopening the
    /// <see cref="MicrosoftBenzeneServiceContainer"/> resolved from the application's root service
    /// provider so further registrations can be added to it.
    /// </summary>
    /// <param name="applicationBuilder">The ASP.NET Core application builder to wire entry point applications into.</param>
    public AspApplicationBuilder(IApplicationBuilder applicationBuilder)
    {
        _applicationBuilder = applicationBuilder;

        var microsoftBenzeneServiceContainer =
            new MicrosoftServiceResolverFactory(applicationBuilder.ApplicationServices)
                    .CreateScope()
                    .GetService<IBenzeneServiceContainer>() as
                MicrosoftBenzeneServiceContainer;

        microsoftBenzeneServiceContainer.Reopen();
        _benzeneServiceContainer = microsoftBenzeneServiceContainer;
    }

    /// <summary>
    /// Registers a factory for an entry point application, adding ASP.NET Core middleware that runs it
    /// against each incoming request.
    /// </summary>
    /// <param name="func">A factory that creates the entry point application given the current invocation's service resolver factory.</param>
    /// <remarks>
    /// The registered middleware calls <c>next()</c> to continue the ASP.NET Core pipeline only if the
    /// response hasn't already started, allowing subsequent middleware (e.g. MVC endpoints) to still
    /// handle requests this entry point application doesn't respond to.
    /// </remarks>
    public void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication<HttpContext>> func)
    {

        var serviceResolverFactory = _benzeneServiceContainer.CreateServiceResolverFactory();
        var entryPoint = func(serviceResolverFactory);

        _applicationBuilder.Use(async (context, next) =>
        {
            await entryPoint.SendAsync(context);
            if (!context.Response.HasStarted)
            {
                await next();
            }
        });

    }

    /// <summary>
    /// Registers services with the underlying service container.
    /// </summary>
    /// <param name="action">The action that performs the registration.</param>
    public void Register(Action<IBenzeneServiceContainer> action)
    {
        action(_benzeneServiceContainer);
    }

    /// <summary>
    /// Creates a new middleware pipeline builder for a given context type, sharing this builder's
    /// underlying service container.
    /// </summary>
    /// <typeparam name="TNewContext">The context type the pipeline operates on.</typeparam>
    /// <returns>The created pipeline builder.</returns>
    public IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>()
    {
        return new MiddlewarePipelineBuilder<TNewContext>(_benzeneServiceContainer);
    }
}
