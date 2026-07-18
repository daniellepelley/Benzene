using System.Linq;
using Benzene.Abstractions.Results;

namespace Benzene.Clients;

/// <summary>
/// Thrown by <see cref="IBenzeneMessageSender.SendAsync{TRequest,TResponse}"/> when the outbound
/// route for a topic produced a response typed for a different <c>TResponse</c> than the caller
/// requested - typically because the topic's transport (e.g. SQS/SNS/Service Bus/Event Hubs/Event
/// Grid/Queue Storage) has no request/response semantics beyond a send acknowledgement, and its
/// route always sets an <see cref="Abstractions.Results.Void"/> response regardless of what
/// <c>TResponse</c> the caller asks for. Replaces a raw <see cref="InvalidCastException"/> at the
/// same call site, which named neither the topic nor the type mismatch.
/// </summary>
public class OutboundResponseTypeMismatchException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundResponseTypeMismatchException"/> class.
    /// </summary>
    /// <param name="topic">The topic that was sent to.</param>
    /// <param name="actualResponseType">
    /// The payload type of the <see cref="IBenzeneResult{T}"/> the route actually produced, or
    /// <see langword="null"/> if it could not be determined (the response was null or not an
    /// <see cref="IBenzeneResult{T}"/> at all).
    /// </param>
    /// <param name="requestedResponseType">The <c>TResponse</c> the caller requested.</param>
    public OutboundResponseTypeMismatchException(string topic, Type? actualResponseType, Type requestedResponseType)
        : base(BuildMessage(topic, actualResponseType, requestedResponseType))
    {
        Topic = topic;
        ActualResponseType = actualResponseType;
        RequestedResponseType = requestedResponseType;
    }

    /// <summary>Gets the topic that was sent to.</summary>
    public string Topic { get; }

    /// <summary>
    /// Gets the payload type of the <see cref="IBenzeneResult{T}"/> the route actually produced, or
    /// <see langword="null"/> if it could not be determined.
    /// </summary>
    public Type? ActualResponseType { get; }

    /// <summary>Gets the <c>TResponse</c> the caller requested.</summary>
    public Type RequestedResponseType { get; }

    private static string BuildMessage(string topic, Type? actualResponseType, Type requestedResponseType)
    {
        return actualResponseType == null
            ? $"Topic '{topic}' produced a response that isn't an IBenzeneResult<{requestedResponseType.Name}> " +
              $"(the requested TResponse). Check the route registered for '{topic}'."
            : $"Topic '{topic}' was sent with TResponse '{requestedResponseType.Name}', but its route produced " +
              $"an IBenzeneResult<{actualResponseType.Name}>. This usually means the topic's transport has no " +
              $"request/response semantics beyond a send acknowledgement (e.g. SQS/SNS/Service Bus/Event Hubs/" +
              $"Event Grid/Queue Storage) - call SendAsync<TRequest, {actualResponseType.Name}>(...) instead.";
    }

    /// <summary>
    /// Extracts the closed <see cref="IBenzeneResult{T}"/> payload type from a response object, for
    /// building the exception message - <see langword="null"/> if <paramref name="response"/> is
    /// <see langword="null"/> or doesn't implement <see cref="IBenzeneResult{T}"/>.
    /// </summary>
    internal static Type? GetActualResponseType(object? response)
    {
        return response?.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBenzeneResult<>))
            ?.GetGenericArguments()[0];
    }
}
