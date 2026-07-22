using Benzene.Mesh.Wire;
using Benzene.Results;

namespace Benzene.Mesh.Collector;

/// <summary>
/// The in-memory state behind the spec collector (docs/specification/mesh.md §4-§6):
/// cumulative per-service and per-topic stats, the latest heartbeat per instance, registered
/// descriptors, and a bounded ring of recent trace events (the window consumer edges and the
/// trace query derive from). Everything is derived - a service that never registered still
/// appears once its traces do (anonymous but live, with its missing feeds named), a registered
/// service with no traffic is a catalog entry with no stats, and no missing feed ever fails
/// ingestion or a query: the §6 degradation rule, collector side.
/// </summary>
public class MeshCollectorStore
{
    private readonly object _lock = new();
    private readonly int _capacity;
    private readonly Dictionary<string, ServiceState> _services = new();
    private readonly Dictionary<(string Id, string Version), TopicState> _topics = new();
    private readonly List<MeshTraceEvent> _ring;
    private int _next;

    private const int MaxFleetTraces = 20;

    public MeshCollectorStore(int maxTraceEvents = 4096)
    {
        _capacity = maxTraceEvents;
        _ring = new List<MeshTraceEvent>(Math.Min(maxTraceEvents, 1024));
    }

    /// <summary>
    /// When this store started accumulating - the window start for anything reporting the
    /// cumulative stats (storage is in-memory, so counts always cover "since process start").
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

    private class ServiceState
    {
        public MeshServiceDescriptor? Descriptor;
        public readonly Dictionary<string, InstanceState> Instances = new();
        public DateTimeOffset LastSeen;
        public long Invocations;
        public long Errors;
    }

    private class InstanceState
    {
        public bool Healthy;
        public DateTimeOffset LastHeartbeat;
        public string? DescriptorHash;
    }

    private class TopicState
    {
        public readonly HashSet<string> Providers = new();
        public readonly Dictionary<string, long> StatusCounts = new();
        public long Invocations;
        public long Errors;
        public double TotalDurationMs;
        public DateTimeOffset LastSeen;
    }

    /// <summary>Stores the descriptor as the service's current contract, replacing any previous
    /// registration wholesale - a redeploy that drops a topic drops the provider claim with it.</summary>
    public void Register(MeshServiceDescriptor descriptor)
    {
        lock (_lock)
        {
            foreach (var topic in _topics.Values)
            {
                topic.Providers.Remove(descriptor.Service);
            }

            var state = EnsureService(descriptor.Service);
            state.Descriptor = descriptor;
            state.LastSeen = DateTimeOffset.UtcNow;

            foreach (var topic in descriptor.Topics)
            {
                EnsureTopic((topic.Id, topic.Version ?? string.Empty)).Providers.Add(descriptor.Service);
            }
        }
    }

    /// <summary>Records the latest health report for one instance.</summary>
    public void Heartbeat(MeshHeartbeat heartbeat)
    {
        lock (_lock)
        {
            var state = EnsureService(heartbeat.Service);
            state.LastSeen = DateTimeOffset.UtcNow;
            state.Instances[heartbeat.InstanceId ?? string.Empty] = new InstanceState
            {
                Healthy = heartbeat.Health?.IsHealthy ?? false,
                LastHeartbeat = DateTimeOffset.UtcNow,
                DescriptorHash = heartbeat.DescriptorHash
            };
        }
    }

    /// <summary>Ingests a trace batch: the bounded ring window plus cumulative stats (which
    /// deliberately outlive the window). Returns how many events were accepted.</summary>
    public int AddEvents(IReadOnlyList<MeshTraceEvent> events)
    {
        lock (_lock)
        {
            foreach (var traceEvent in events)
            {
                if (_ring.Count < _capacity)
                {
                    _ring.Add(traceEvent);
                }
                else
                {
                    _ring[_next] = traceEvent;
                    _next = (_next + 1) % _capacity;
                }

                var failed = !BenzeneResultStatusExtensions.IsSuccess(traceEvent.Status);

                // A wire payload can carry a null status; coalesce it (like TopicVersion above) so it
                // never reaches the Dictionary key path as null (ArgumentNullException would abort the
                // whole batch mid-loop, against the §6 "no feed fails ingestion" rule).
                var status = traceEvent.Status ?? string.Empty;
                var topic = EnsureTopic((traceEvent.Topic, traceEvent.TopicVersion ?? string.Empty));
                topic.Invocations++;
                topic.StatusCounts[status] = topic.StatusCounts.GetValueOrDefault(status) + 1;
                topic.TotalDurationMs += traceEvent.DurationMs;
                topic.LastSeen = DateTimeOffset.UtcNow;
                if (failed)
                {
                    topic.Errors++;
                }

                if (!string.IsNullOrEmpty(traceEvent.Service))
                {
                    var service = EnsureService(traceEvent.Service!);
                    service.Invocations++;
                    service.LastSeen = DateTimeOffset.UtcNow;
                    if (failed)
                    {
                        service.Errors++;
                    }
                }
            }
            return events.Count;
        }
    }

    public FleetView Fleet()
    {
        lock (_lock)
        {
            var consumers = ConsumersByTopic();
            return new FleetView
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                Services = _services.Keys.OrderBy(x => x, StringComparer.Ordinal).Select(ServiceSummaryLocked).ToList(),
                Topics = _topics.Keys
                    .OrderBy(x => x.Id, StringComparer.Ordinal).ThenBy(x => x.Version, StringComparer.Ordinal)
                    .Select(key => TopicSummaryLocked(key, consumers.GetValueOrDefault(key)))
                    .ToList(),
                Traces = TraceSummariesLocked(MaxFleetTraces)
            };
        }
    }

    public ServiceView? Service(string name)
    {
        lock (_lock)
        {
            if (!_services.TryGetValue(name, out var state))
            {
                return null;
            }

            var summary = ServiceSummaryLocked(name);
            var view = new ServiceView
            {
                Service = summary.Service,
                Runtime = summary.Runtime,
                Binding = summary.Binding,
                Placement = summary.Placement,
                Topics = summary.Topics,
                Health = summary.Health,
                LastSeen = summary.LastSeen,
                Invocations = summary.Invocations,
                Errors = summary.Errors,
                MissingFeeds = summary.MissingFeeds,
                Descriptor = state.Descriptor,
                Instances = state.Instances
                    .OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(pair => new InstanceView
                    {
                        InstanceId = pair.Key,
                        Healthy = pair.Value.Healthy,
                        LastHeartbeat = pair.Value.LastHeartbeat,
                        DescriptorHash = pair.Value.DescriptorHash,
                        HashMatches = state.Descriptor?.DescriptorHash != null && pair.Value.DescriptorHash != null
                            ? pair.Value.DescriptorHash == state.Descriptor.DescriptorHash
                            : null
                    })
                    .ToList()
            };
            return view;
        }
    }

    public TopicSummary? Topic(string id, string? version)
    {
        lock (_lock)
        {
            var key = (id, version ?? string.Empty);
            if (!_topics.ContainsKey(key))
            {
                return null;
            }
            return TopicSummaryLocked(key, ConsumersByTopic().GetValueOrDefault(key));
        }
    }

    public TraceView? Trace(string traceId)
    {
        lock (_lock)
        {
            var events = _ring.Where(x => x.TraceId == traceId).OrderBy(x => x.StartedAt).ToList();
            return events.Count == 0 ? null : new TraceView { TraceId = traceId, Events = events };
        }
    }

    private ServiceState EnsureService(string name)
    {
        if (!_services.TryGetValue(name, out var state))
        {
            state = new ServiceState();
            _services[name] = state;
        }
        return state;
    }

    private TopicState EnsureTopic((string Id, string Version) key)
    {
        if (!_topics.TryGetValue(key, out var state))
        {
            state = new TopicState();
            _topics[key] = state;
        }
        return state;
    }

    /// <summary>Derives who-calls-whom from the ring window: an event whose parent span belongs to
    /// another service makes that service a consumer of the event's topic (spec §4). Unmeshed
    /// callers have no parent span in the window and produce no edge - never a guess.</summary>
    private Dictionary<(string Id, string Version), HashSet<string>> ConsumersByTopic()
    {
        var spanService = new Dictionary<string, string>();
        foreach (var traceEvent in _ring)
        {
            if (!string.IsNullOrEmpty(traceEvent.Service))
            {
                spanService[traceEvent.SpanId] = traceEvent.Service!;
            }
        }

        var consumers = new Dictionary<(string, string), HashSet<string>>();
        foreach (var traceEvent in _ring)
        {
            if (string.IsNullOrEmpty(traceEvent.ParentSpanId) ||
                !spanService.TryGetValue(traceEvent.ParentSpanId!, out var caller) ||
                caller == traceEvent.Service)
            {
                continue;
            }
            var key = (traceEvent.Topic, traceEvent.TopicVersion ?? string.Empty);
            if (!consumers.TryGetValue(key, out var set))
            {
                set = new HashSet<string>();
                consumers[key] = set;
            }
            set.Add(caller);
        }
        return consumers;
    }

    private ServiceSummary ServiceSummaryLocked(string name)
    {
        var state = _services[name];
        var summary = new ServiceSummary
        {
            Service = name,
            Health = MeshHealth.Unknown,
            LastSeen = state.LastSeen,
            Instances = state.Instances.Count,
            Invocations = state.Invocations,
            Errors = state.Errors
        };

        if (state.Descriptor != null)
        {
            summary.Runtime = state.Descriptor.Runtime;
            summary.Binding = state.Descriptor.Binding;
            summary.Placement = state.Descriptor.Placement;
            summary.Topics = state.Descriptor.Topics.Count;
        }
        else
        {
            summary.MissingFeeds.Add("descriptor"); // known only from traffic: anonymous but live
        }

        if (state.Instances.Count == 0)
        {
            summary.MissingFeeds.Add("health");
        }
        else
        {
            summary.Health = state.Instances.Values.All(x => x.Healthy) ? MeshHealth.Healthy : MeshHealth.Degraded;
        }

        if (state.Invocations == 0)
        {
            summary.MissingFeeds.Add("traces");
        }
        return summary;
    }

    private TopicSummary TopicSummaryLocked((string Id, string Version) key, HashSet<string>? consumers)
    {
        var state = _topics[key];
        return new TopicSummary
        {
            Topic = key.Id,
            Version = string.IsNullOrEmpty(key.Version) ? null : key.Version,
            Providers = state.Providers.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            Consumers = (consumers ?? new HashSet<string>()).OrderBy(x => x, StringComparer.Ordinal).ToList(),
            Invocations = state.Invocations,
            Errors = state.Errors,
            AvgDurationMs = state.Invocations > 0 ? state.TotalDurationMs / state.Invocations : 0,
            StatusCounts = state.StatusCounts.ToDictionary(x => x.Key, x => x.Value),
            LastSeen = state.LastSeen
        };
    }

    private List<TraceSummary> TraceSummariesLocked(int limit)
    {
        return _ring
            .GroupBy(x => x.TraceId)
            .Select(group =>
            {
                var startedAt = group.Min(x => x.StartedAt);
                var end = group.Max(x => x.StartedAt + TimeSpan.FromMilliseconds(x.DurationMs));
                return new TraceSummary
                {
                    TraceId = group.Key,
                    Events = group.Count(),
                    Services = group.Where(x => !string.IsNullOrEmpty(x.Service))
                        .Select(x => x.Service!).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList(),
                    StartedAt = startedAt,
                    DurationMs = (end - startedAt).TotalMilliseconds,
                    Failed = group.Any(x => !BenzeneResultStatusExtensions.IsSuccess(x.Status))
                };
            })
            .OrderByDescending(x => x.StartedAt)
            .Take(limit)
            .ToList();
    }
}

/// <summary>The wire-contracts §3 success class, applied to a trace event's status: an unknown or
/// empty status counts as a failure, matching every per-protocol mapping table's default.</summary>
public static class BenzeneResultStatusExtensions
{
    private static readonly HashSet<string> SuccessStatuses = new()
    {
        BenzeneResultStatus.Ok,
        BenzeneResultStatus.Created,
        BenzeneResultStatus.Accepted,
        BenzeneResultStatus.Updated,
        BenzeneResultStatus.Deleted,
        BenzeneResultStatus.Ignored
    };

    public static bool IsSuccess(string? status)
    {
        return status != null && SuccessStatuses.Contains(status);
    }
}
