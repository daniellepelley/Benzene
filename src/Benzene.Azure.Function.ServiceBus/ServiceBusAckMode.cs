namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Configures whether a Service Bus message's completion is left to the Azure Functions host's own
/// auto-complete behavior, or explicitly controlled based on the message handler's outcome.
/// </summary>
public enum ServiceBusAckMode
{
    /// <summary>
    /// The trigger completes the message automatically once the function returns without throwing
    /// (today's default, unchanged) - Benzene never calls <c>CompleteMessageAsync</c>/
    /// <c>AbandonMessageAsync</c> itself. Requires no additional trigger configuration.
    /// </summary>
    AutoComplete = 0,

    /// <summary>
    /// Benzene calls <c>ServiceBusMessageActions.CompleteMessageAsync</c> after a successful outcome
    /// and <c>AbandonMessageAsync</c> after a failed one (an unhandled exception, or a non-exception
    /// failure result), for each message individually. Requires the trigger to set
    /// <c>AutoCompleteMessages = false</c> and the trigger function to call the
    /// <c>HandleServiceBusMessages</c> overload that accepts a <c>ServiceBusMessageActions</c>
    /// parameter - see <c>Benzene.Azure.Function.ServiceBus.Extensions.HandleServiceBusMessages</c>.
    /// </summary>
    Explicit = 1,
}
