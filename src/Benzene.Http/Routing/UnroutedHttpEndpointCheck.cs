using System.Reflection;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Exceptions;
using Benzene.Core.MessageHandlers;

namespace Benzene.Http.Routing;

/// <summary>
/// Fail-fast diagnostic for the silent-404 trap: a message handler carrying
/// <see cref="HttpEndpointAttribute"/> but no <c>[Message]</c> attribute is skipped by handler
/// discovery entirely, so its route is never registered and requests to it 404 with no hint why.
/// This finder contributes no endpoints; it cross-references the candidate types every
/// reflection-scanning <c>AddMessageHandlers</c> call inspected
/// (<see cref="MessageHandlerCandidateTypes"/>) against the discovered handler definitions, and
/// throws a <see cref="BenzeneException"/> naming each unrouted handler when the route table is
/// first built — instead of letting the app run with the route silently missing.
/// </summary>
/// <remarks>
/// A handler registered explicitly with a topic (e.g. <c>AddMessageHandler&lt;THandler, TRequest,
/// TResponse&gt;("topic")</c>) appears among the discovered definitions and is not flagged, so the
/// attribute-less-but-explicitly-registered pattern keeps working.
/// </remarks>
public class UnroutedHttpEndpointCheck : IHttpEndpointFinder
{
    private readonly MessageHandlerCandidateTypes[] _candidateTypes;
    private readonly IMessageHandlersFinder _messageHandlersFinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnroutedHttpEndpointCheck"/> class.
    /// </summary>
    /// <param name="candidateTypes">The candidate-type records from every reflection-scanning <c>AddMessageHandlers</c> call.</param>
    /// <param name="messageHandlersFinder">The finder whose discovered definitions the candidates are checked against.</param>
    public UnroutedHttpEndpointCheck(IEnumerable<MessageHandlerCandidateTypes> candidateTypes,
        IMessageHandlersFinder messageHandlersFinder)
    {
        _candidateTypes = candidateTypes.ToArray();
        _messageHandlersFinder = messageHandlersFinder;
    }

    /// <summary>
    /// Contributes no endpoint definitions; throws if any scanned handler type has an
    /// <see cref="HttpEndpointAttribute"/> but was skipped by handler discovery.
    /// </summary>
    /// <returns>An empty array when every <c>[HttpEndpoint]</c>-carrying handler is routable.</returns>
    /// <exception cref="BenzeneException">
    /// Thrown when a scanned, non-abstract handler class carries <see cref="HttpEndpointAttribute"/>
    /// but no <c>[Message]</c> attribute and no explicit registration, so no route exists for it.
    /// </exception>
    public IHttpEndpointDefinition[] FindDefinitions()
    {
        var discovered = new HashSet<Type>(_messageHandlersFinder.FindDefinitions().Select(x => x.HandlerType));

        var unrouted = _candidateTypes
            .SelectMany(x => x.Types)
            .Distinct()
            .Where(x => x is { IsClass: true, IsAbstract: false })
            .Where(IsMessageHandler)
            .Where(x => x.GetCustomAttributes<HttpEndpointAttribute>().Any())
            .Where(x => !x.GetCustomAttributes<MessageAttribute>().Any())
            .Where(x => !discovered.Contains(x))
            .ToArray();

        if (unrouted.Length > 0)
        {
            var names = string.Join(", ", unrouted.Select(x => x.FullName));
            throw new BenzeneException(
                $"The following message handler(s) have [HttpEndpoint] but no [Message] attribute, so handler discovery skips them and their HTTP route(s) do not exist: {names}. " +
                "Add a [Message(\"topic\")] attribute to each handler, or register it explicitly with a topic (e.g. AddMessageHandler<THandler, TRequest, TResponse>(\"topic\")).");
        }

        return Array.Empty<IHttpEndpointDefinition>();
    }

    private static bool IsMessageHandler(Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && (
            i.GetGenericTypeDefinition() == typeof(IMessageHandler<,>) ||
            i.GetGenericTypeDefinition() == typeof(IMessageHandler<>)));
    }
}
