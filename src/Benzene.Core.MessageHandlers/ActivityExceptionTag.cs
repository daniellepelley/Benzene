using System.Diagnostics;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Stamps a caught exception's TYPE onto the current dispatch's topic-bearing span
/// (<c>benzene.exception.type</c>), so a trace shows WHY a message failed, not just that it did.
/// </summary>
/// <remarks>
/// The handler layer converts exceptions into results (<see cref="MessageHandler{TRequest,TResponse}"/>),
/// so the exception never propagates to the tracing decorator's own catch — by the time
/// <c>benzene.status</c> (e.g. <c>service-unavailable</c>) is stamped after <c>next()</c>, the exception
/// object is gone. This helper records the type at the catch site instead: it walks the open ancestor
/// chain of <see cref="Activity.Current"/> for the span carrying <c>benzene.topic</c> (the single span
/// per dispatch that Benzene.Diagnostics' once-guard stamps the message-identity tags on) and tags it,
/// first exception wins. Type name only — never the message or stack trace: the type is the stable,
/// non-sensitive discriminator, the same classification rule the health-check plane applies. Span-only
/// by design, never a metric tag (unbounded cardinality). No-ops when tracing isn't active (no listener
/// → <see cref="Activity.Current"/> is null) or no topic-bearing ancestor exists.
/// </remarks>
internal static class ActivityExceptionTag
{
    internal const string TagName = "benzene.exception.type";

    public static void TryStamp(Exception exception)
    {
        for (var activity = Activity.Current; activity != null; activity = activity.Parent)
        {
            if (activity.GetTagItem("benzene.topic") == null)
            {
                continue;
            }

            if (activity.GetTagItem(TagName) == null)
            {
                activity.SetTag(TagName, exception.GetType().FullName);
            }

            return;
        }
    }
}
