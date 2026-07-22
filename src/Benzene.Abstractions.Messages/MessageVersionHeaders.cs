namespace Benzene.Abstractions.Messages;

/// <summary>
/// Well-known names for the header/attribute that carries a message's payload schema version on the wire.
/// <para>
/// The inbound reader (<c>HeaderMessageVersionGetter</c>) tries a fallback list; <see cref="Default"/> is
/// its primary name and the one the outbound helpers write, so a producer and consumer agree on the
/// version signal without hard-coding the literal in two places. HTTP additionally carries the version as
/// a <c>/v{version}</c> route segment (see <c>docs/specification/versioning.md</c>); this header is the
/// convention for every other transport (and the HTTP fallback).
/// </para>
/// </summary>
public static class MessageVersionHeaders
{
    /// <summary>The canonical version header/attribute name: <c>benzene-version</c>.</summary>
    public const string Default = "benzene-version";
}
