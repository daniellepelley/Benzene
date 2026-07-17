using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Auth.Core;
using Benzene.Core.Middleware;
using Benzene.Http;

namespace Benzene.Auth.Basic;

/// <summary>
/// Provides extension methods for configuring RFC 7617 HTTP Basic authentication in the Benzene pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds RFC 7617 HTTP Basic authentication middleware to the pipeline. Requests without a
    /// valid <c>Authorization: Basic</c> header, or whose decoded credentials fail
    /// <paramref name="validator"/>, are short-circuited with <c>Unauthorized</c> (and a
    /// <c>WWW-Authenticate</c> challenge header); requests that pass have
    /// <see cref="AuthenticationHolder.Principal"/> set for later pipeline steps and continue to
    /// <c>next()</c>.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="validator">Validates the decoded username/password against the app's own credential store.</param>
    /// <param name="realm">The RFC 7617 realm advertised in the <c>WWW-Authenticate</c> challenge. Defaults to <c>"Benzene"</c>.</param>
    /// <returns>The middleware pipeline builder for method chaining.</returns>
    /// <remarks>
    /// Registers <see cref="AuthenticationHolder"/> scoped in this extension (not centrally) -
    /// the Context Purity pattern (see <c>Benzene.Auth.Core/CLAUDE.md</c>): a pipeline that never
    /// calls <c>UseBasicAuth</c>/<c>UseOAuth2Bearer</c> never allocates a holder anyone would look at.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseBasicAuth<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, IBasicAuthCredentialValidator validator, string realm = "Benzene")
        where TContext : IHttpContext
    {
        app.Register(x =>
        {
            x.TryAddScoped<AuthenticationHolder>();
            x.AddScoped(resolver => new BasicAuthMiddleware<TContext>(
                validator, realm,
                resolver.GetService<IHttpRequestAdapter<TContext>>(),
                resolver.GetService<IBenzeneResponseAdapter<TContext>>(),
                resolver.GetService<AuthenticationHolder>(),
                resolver
            ));
        });

        return app.Use<TContext, BasicAuthMiddleware<TContext>>();
    }
}
