using System;
using System.Collections.Generic;
using System.Diagnostics;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Diagnostics;

/// <summary>
/// The handler-converted-exception path of <c>benzene.exception.type</c>: the handler layer turns a
/// thrown exception into a result, so the tracing decorator's own catch never sees it — instead
/// <see cref="ActivityExceptionTag"/> stamps the exception's TYPE onto the topic-bearing ancestor span
/// (the one the once-guard gave <c>benzene.topic</c>) at the catch site, walking up from
/// <see cref="Activity.Current"/>.
/// </summary>
public class ActivityExceptionTagTest : IDisposable
{
    private readonly List<Activity> _started = new();

    private Activity Start(string name, bool topicBearing = false)
    {
        var activity = new Activity(name);
        if (topicBearing)
        {
            activity.SetTag("benzene.topic", "orders:create");
        }
        activity.Start(); // becomes Activity.Current, parented to the previous Current
        _started.Add(activity);
        return activity;
    }

    public void Dispose()
    {
        for (var i = _started.Count - 1; i >= 0; i--)
        {
            _started[i].Stop();
        }
    }

    [Fact]
    public void TryStamp_TagsTheTopicBearingAncestor_NotTheCurrentSpan()
    {
        var topicSpan = Start("outer", topicBearing: true);
        var inner = Start("handler-middleware");

        ActivityExceptionTag.TryStamp(new InvalidOperationException("boom"));

        Assert.Equal("System.InvalidOperationException", topicSpan.GetTagItem("benzene.exception.type"));
        Assert.Null(inner.GetTagItem("benzene.exception.type")); // only the topic-bearing span carries it
    }

    [Fact]
    public void TryStamp_FirstExceptionWins()
    {
        var topicSpan = Start("outer", topicBearing: true);
        Start("inner");

        ActivityExceptionTag.TryStamp(new InvalidOperationException("first"));
        ActivityExceptionTag.TryStamp(new ArgumentException("second"));

        // The first failure is the interesting one; a later conversion never overwrites it.
        Assert.Equal("System.InvalidOperationException", topicSpan.GetTagItem("benzene.exception.type"));
    }

    [Fact]
    public void TryStamp_NoTopicBearingAncestor_IsANoOp()
    {
        var plain = Start("no-topic");

        ActivityExceptionTag.TryStamp(new InvalidOperationException("boom"));

        Assert.Null(plain.GetTagItem("benzene.exception.type"));
    }

    [Fact]
    public void TryStamp_NoActivityAtAll_DoesNotThrow()
    {
        // Tracing not active (no listener) → Activity.Current is null → silently nothing.
        ActivityExceptionTag.TryStamp(new InvalidOperationException("boom"));
    }
}
