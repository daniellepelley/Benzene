namespace Benzene.Http.BenzeneMessage;

/// <summary>
/// Options for the BenzeneMessage-over-HTTP endpoint (see
/// <see cref="Extensions.UseBenzeneMessage{TContext}(Benzene.Abstractions.Middleware.IMiddlewarePipelineBuilder{TContext}, System.Action{Benzene.Abstractions.Middleware.IMiddlewarePipelineBuilder{Benzene.Core.Messages.BenzeneMessage.BenzeneMessageContext}})"/>).
/// </summary>
public class BenzeneMessageHttpOptions
{
    /// <summary>The default path the endpoint listens on.</summary>
    public const string DefaultPath = "/benzene-message";

    /// <summary>
    /// Gets or sets the path the endpoint listens on for POSTed BenzeneMessage envelopes.
    /// Defaults to <see cref="DefaultPath"/>.
    /// </summary>
    public string Path { get; set; } = DefaultPath;

    /// <summary>
    /// Gets or sets an optional topic allowlist predicate. When set, an envelope whose topic the
    /// predicate rejects is answered with a <c>NotFound</c> envelope instead of being dispatched.
    /// When null (the default), every topic is dispatched.
    /// </summary>
    public Func<string, bool>? TopicFilter { get; set; }
}
