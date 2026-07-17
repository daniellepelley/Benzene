using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

namespace Benzene.Auth.Core;

/// <summary>
/// Shared "middleware short-circuits with a status + detail message" helper for authentication/
/// authorization middleware, matching the exact idiom already used elsewhere in this codebase
/// (see <c>Benzene.HealthChecks</c>' <c>UseHealthCheckMiddleware</c> and <c>Benzene.Http</c>'s
/// <c>CorsMiddleware</c>) for applying an <see cref="Benzene.Abstractions.Results.IBenzeneResult"/>
/// through <see cref="IMessageHandlerResultSetter{TContext}"/> - this invents no new wire shape.
/// The <c>detail</c> string ends up as <c>ErrorPayload.Detail</c> (see
/// <c>docs/specification/wire-contracts.md</c> §1.3/§3): <see cref="BenzeneResult.Unauthorized(string[])"/>/
/// <see cref="BenzeneResult.Forbidden(string[])"/> attach it as the result's single error, which
/// <c>ErrorPayload</c>'s constructor joins into <c>Detail</c>.
/// </summary>
public static class AuthResults
{
    /// <summary>
    /// Short-circuits the current message with <c>Unauthorized</c> ("caller not authenticated" -
    /// see wire-contracts.md §3) and the given detail message.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="resolver">The current message's service resolver, as received by a <c>Use(resolver => ...)</c> middleware factory.</param>
    /// <param name="context">The transport-specific context for the message being handled.</param>
    /// <param name="detail">A human-readable reason, safe to return to the caller (never echo internal validation detail - see <c>Benzene.Auth.OAuth2/CLAUDE.md</c>).</param>
    public static Task UnauthorizedAsync<TContext>(IServiceResolver resolver, TContext context, string detail)
    {
        return SetResultAsync(resolver, context, BenzeneResult.Unauthorized(detail));
    }

    /// <summary>
    /// Short-circuits the current message with <c>Forbidden</c> ("caller authenticated but not
    /// permitted" - see wire-contracts.md §3) and the given detail message.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="resolver">The current message's service resolver, as received by a <c>Use(resolver => ...)</c> middleware factory.</param>
    /// <param name="context">The transport-specific context for the message being handled.</param>
    /// <param name="detail">A human-readable reason, safe to return to the caller.</param>
    public static Task ForbiddenAsync<TContext>(IServiceResolver resolver, TContext context, string detail)
    {
        return SetResultAsync(resolver, context, BenzeneResult.Forbidden(detail));
    }

    private static Task SetResultAsync<TContext>(IServiceResolver resolver, TContext context, Benzene.Abstractions.Results.IBenzeneResult benzeneResult)
    {
        var resultSetter = resolver.GetService<IMessageHandlerResultSetter<TContext>>();

        // No specific handler ran - this middleware short-circuited before routing got that far -
        // so report the real incoming topic (same idiom as UseHealthCheckMiddleware) with
        // MessageHandlerDefinition.Empty() rather than inventing a synthetic topic the way
        // CorsMiddleware does for its own "cors" preflight result.
        var messageGetter = resolver.GetService<IMessageGetter<TContext>>();
        var topic = messageGetter.GetTopic(context);

        return resultSetter.SetResultAsync(context, new MessageHandlerResult(topic, MessageHandlerDefinition.Empty(), benzeneResult));
    }
}
