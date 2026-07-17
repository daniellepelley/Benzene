using System.Security.Claims;

namespace Benzene.Auth.Core;

/// <summary>
/// Carries the current message's authenticated <see cref="ClaimsPrincipal"/>, set by whichever
/// authentication middleware ran for this message (e.g. <c>UseBasicAuth</c>/<c>UseOAuth2Bearer</c>)
/// and read back by later pipeline steps - a handler that wants the caller, or a downstream
/// authorization check like <c>RequireScope</c>.
/// </summary>
/// <remarks>
/// Registered scoped (one instance per message, alongside the rest of that message's DI scope) -
/// deliberately NOT carried on <c>TContext</c>. A context type describes the shape of a transport
/// message; it should stay free of optional, cross-cutting state that only some pipelines opt into.
/// Scoped DI state is the seam for that instead - the same "Context purity" pattern
/// <c>Benzene.Core.MessageHandlers</c>' <c>PresetTopicHolder</c> follows (see
/// <c>Benzene.Abstractions.Middleware/CLAUDE.md</c>'s "Context purity" section for the general
/// rule). A pipeline that never adds an authentication middleware never even allocates a holder
/// anyone would look at, and <see cref="Principal"/> simply stays <c>null</c>.
///
/// No interface: app code that wants to read the caller reads this type directly, same as
/// <c>PresetTopicHolder</c> is read directly rather than through an abstraction.
/// <see cref="ClaimsPrincipal"/>/<see cref="ClaimsIdentity"/> (BCL, <c>System.Security.Claims</c>)
/// is the payload type - no Benzene-specific "principal" abstraction; every JWT/OAuth2 library
/// already produces this shape, and inventing a wrapper would buy nothing.
/// </remarks>
public class AuthenticationHolder
{
    /// <summary>
    /// Gets or sets the authenticated caller for the current message, or <c>null</c> if no
    /// authentication middleware ran, or if the one that ran failed authentication.
    /// </summary>
    public ClaimsPrincipal? Principal { get; set; }
}
