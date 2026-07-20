using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Abstracts the settle operations shared by the SDK's <see cref="ProcessMessageEventArgs"/> (regular
/// processor) and <see cref="ProcessSessionMessageEventArgs"/> (session processor), which expose the
/// same complete/abandon/dead-letter/defer surface but are unrelated types. Lets
/// <see cref="BenzeneServiceBusWorker"/> settle a message the same way regardless of which processor
/// delivered it.
/// </summary>
internal interface IServiceBusMessageSettler
{
    /// <summary>The received message being settled.</summary>
    ServiceBusReceivedMessage Message { get; }

    /// <summary>The delivery's cancellation token.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Completes the message.</summary>
    Task CompleteMessageAsync();

    /// <summary>Abandons the message for redelivery.</summary>
    Task AbandonMessageAsync();

    /// <summary>Dead-letters the message with the given reason/description.</summary>
    Task DeadLetterMessageAsync(string? reason, string? description);

    /// <summary>Defers the message.</summary>
    Task DeferMessageAsync();
}
