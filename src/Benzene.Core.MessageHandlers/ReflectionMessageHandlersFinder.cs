using System.Diagnostics;
using System.Reflection;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Exceptions;
using Void = Benzene.Results.Void;

namespace Benzene.Core.MessageHandlers;

public class ReflectionMessageHandlersFinder : IMessageHandlersFinder
{
    private readonly Type[] _types;

    public ReflectionMessageHandlersFinder(params Assembly[] assemblies)
    {
        _types = Utils.GetAllTypes(assemblies).ToArray();
    }
    
    public ReflectionMessageHandlersFinder(params Type[] types)
    {
        _types = types;
    }

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
                    Debug.WriteLine($"{messageHandlerInterface.FullName} does not have a topic attribute");
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
