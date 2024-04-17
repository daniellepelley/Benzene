using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Exceptions;
using Benzene.Core.Helper;
using Void = Benzene.Results.Void;

namespace Benzene.Core.MessageHandling;

public class ReflectionMessageSendersFinder : IMessageSendersFinder
{
    private readonly Type[] _types;

    public ReflectionMessageSendersFinder(params Assembly[] assemblies)
    {
        _types = Utils.GetAllTypes(assemblies).ToArray();
    }
    
    public ReflectionMessageSendersFinder(params Type[] types)
    {
        _types = types;
    }

    public IMessageSenderDefinition[] FindDefinitions()
    {
        var handlers = _types
            .Select(m =>
            {
                var messageHandlerInterface = m.GetInterfaces().FirstOrDefault(i =>
                    i.IsGenericType && (
                        i.GetGenericTypeDefinition() == typeof(IMessageSender<,>) ||
                        i.GetGenericTypeDefinition() == typeof(IMessageSender<>)));

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

                return new
                {
                    attribute.Topic,
                    attribute.Version,
                    HandlerType = m,
                    MessageType = genericArguments[0],
                    ResponseType = genericArguments.Length > 1 ? genericArguments[1] : typeof(Void)
                };
            })
            .Where(x => x != null)
            .OrderBy(x => x.Topic)
            .ToArray();

        var duplicates = handlers
            .GroupBy(x => new { x.Topic, x.Version})
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

        return handlers
            .Select(x => MessageSenderDefinition.CreateInstance(x.Topic,
                    x.Version,
                    x.MessageType,
                    x.ResponseType,
                    x.HandlerType)
             as IMessageSenderDefinition)
            .ToArray();
    }
}
