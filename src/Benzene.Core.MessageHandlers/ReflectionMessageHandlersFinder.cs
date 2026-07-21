using System.Reflection;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Exceptions;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Discovers <see cref="IMessageHandlerDefinition"/>s by scanning types for the <see cref="MessageAttribute"/>
/// and either <c>IMessageHandler&lt;TRequest&gt;</c> or <c>IMessageHandler&lt;TRequest, TResponse&gt;</c>.
/// </summary>
/// <remarks>
/// Types without a <see cref="MessageAttribute"/> are skipped (with a debug trace), even if they
/// implement a handler interface. If more than one discovered type is registered against the same
/// topic id + version, <see cref="FindDefinitions"/> throws a <see cref="BenzeneException"/> rather
/// than silently picking one, since that would make routing non-deterministic.
/// </remarks>
public class ReflectionMessageHandlersFinder : IMessageHandlersFinder
{
    private readonly Type[] _types;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionMessageHandlersFinder"/> class that
    /// scans every non-dynamic type across the given assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handler types.</param>
    public ReflectionMessageHandlersFinder(params Assembly[] assemblies)
    {
        _types = Utils.GetAllTypes(assemblies).ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionMessageHandlersFinder"/> class that
    /// scans only the given, already-known candidate types.
    /// </summary>
    /// <param name="types">The candidate types to inspect for handler interfaces and <see cref="MessageAttribute"/>.</param>
    public ReflectionMessageHandlersFinder(params Type[] types)
    {
        _types = types;
    }

    /// <summary>
    /// Scans the configured types for handler definitions.
    /// </summary>
    /// <returns>Every distinct handler definition found.</returns>
    /// <exception cref="BenzeneException">
    /// Thrown if two or more discovered handler types are registered against the same topic id and version.
    /// </exception>
    public IMessageHandlerDefinition[] FindDefinitions()
    {
        var handlers = _types
            .Select(m =>
            {
                var messageHandlerInterface = m.GetInterfaces().FirstOrDefault(i =>
                    i.IsGenericType && (
                        i.GetGenericTypeDefinition() == typeof(IMessageHandler<,>) ||
                        i.GetGenericTypeDefinition() == typeof(IMessageHandler<>)));

                if (messageHandlerInterface == null)
                {
                    return null;
                }

                var attribute = m.GetCustomAttributes<MessageAttribute>().FirstOrDefault();

                if (attribute == null)
                {
                    return null;
                }

                var genericArguments = messageHandlerInterface.GetGenericArguments();

                return MessageHandlerDefinition.CreateInstance(
                    attribute.Topic,
                    attribute.Version,
                    genericArguments[0],
                    genericArguments.Length > 1 ? genericArguments[1] : typeof(Void),
                    m
                ) as IMessageHandlerDefinition;
            })
            .Where(x => x != null)
            .OrderBy(x => x.Topic.Id)
            .GroupBy(x => new { x.Topic.Id, x.Topic.Version, x.HandlerType.AssemblyQualifiedName })
            .Select(x => x.First())
            .ToArray();

        var duplicates = handlers
            .GroupBy(x => new { x.Topic.Id, x.Topic.Version })
            .Where(x => x.Count() > 1)

            .ToArray();

        if (duplicates.Any())
        {
            if (duplicates.Length == 1)
            {
                throw new BenzeneException(
                    $"Topic '{duplicates[0].Key}' has been assigned to more than one message handler, this is not permitted");
            }

            throw new BenzeneException($"Topics '{string.Join(", ", duplicates.Select(x => x.Key))}' have been assigned to more than one message handler, this is not permitted");
        }

        return handlers;
    }

}
