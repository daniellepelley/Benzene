using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Mesh.Wire;

namespace Benzene.CloudService;

/// <summary>
/// The single source of this service's ServiceDescriptor (mesh.md §2) for the reserved-topic
/// middleware and the announcer. Built once: eagerly at wire-up when the handler types were given
/// explicitly, otherwise lazily from the container's registry on first use. Both paths stamp the
/// profile self-assessment onto the descriptor (mesh.md §2's <c>profile</c> field, excluded from
/// the contract hash).
/// </summary>
internal sealed class CloudServiceDescriptorSource
{
    private readonly object _gate = new();
    private readonly MeshServiceInfo _info;
    private readonly CloudServiceProfileReport _report;
    private MeshServiceDescriptor? _descriptor;

    public CloudServiceDescriptorSource(MeshServiceInfo info, CloudServiceProfileReport report, Type[]? handlerTypes)
    {
        _info = info;
        _report = report;

        if (handlerTypes != null)
        {
            _descriptor = Build(new ListLookUp(new ReflectionMessageHandlersFinder(handlerTypes).FindDefinitions()));
        }
    }

    /// <summary>The descriptor, if it has been built yet; the eager path always has one.</summary>
    public MeshServiceDescriptor? TryGet()
    {
        return _descriptor;
    }

    /// <summary>The descriptor, building it from the invocation's registry on first use if needed.</summary>
    public MeshServiceDescriptor Get(IServiceResolver resolver)
    {
        if (_descriptor != null)
        {
            return _descriptor;
        }

        lock (_gate)
        {
            return _descriptor ??= Build(resolver.TryGetService<IMessageHandlerDefinitionLookUp>());
        }
    }

    private MeshServiceDescriptor Build(IMessageHandlerDefinitionLookUp? lookUp)
    {
        var descriptor = MeshDescriptorFactory.Create(lookUp, _info);
        descriptor.Profile = _report.ToMeshProfile();
        return descriptor;
    }

    private sealed class ListLookUp : IMessageHandlerDefinitionLookUp
    {
        private readonly IMessageHandlerDefinition[] _definitions;

        public ListLookUp(IMessageHandlerDefinition[] definitions)
        {
            _definitions = definitions;
        }

        public IMessageHandlerDefinition? FindHandler(ITopic topic)
        {
            return _definitions.FirstOrDefault(x => x.Topic.Id == topic.Id && x.Topic.Version == topic.Version);
        }

        public IMessageHandlerDefinition[] GetAllHandlers()
        {
            return _definitions;
        }
    }
}
