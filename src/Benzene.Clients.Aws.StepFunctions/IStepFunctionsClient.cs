using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;

namespace Benzene.Clients.Aws.StepFunctions;

/// <summary>
/// Represents a client for starting AWS Step Functions state machine executions.
/// </summary>
public interface IStepFunctionsClient : IDisposable
{
    /// <summary>
    /// Starts a new execution of the state machine with the given message as its input.
    /// </summary>
    /// <typeparam name="TMessage">The type of the input message.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="message">The message to serialize as the execution input.</param>
    /// <returns>A task that resolves to the result of starting the execution.</returns>
    Task<IBenzeneResult<TResponse>> StartExecutionAsync<TMessage, TResponse>(TMessage message);

    /// <summary>
    /// Starts a new execution using <paramref name="executionName"/> as the execution's idempotency
    /// name (<c>StartExecutionRequest.Name</c>). Step Functions treats a repeated name for the same
    /// state machine and input as idempotent, so a retry after a lost response won't start a duplicate
    /// execution - the repeat is reported as success. Supply a stable token (e.g. a correlation id).
    /// </summary>
    /// <typeparam name="TMessage">The type of the input message.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="message">The message to serialize as the execution input.</param>
    /// <param name="executionName">
    /// The idempotency name; sanitized to Step Functions' allowed name charset and length. When
    /// <c>null</c> or empty, behaves like <see cref="StartExecutionAsync{TMessage,TResponse}(TMessage)"/>
    /// (AWS generates a UUID name).
    /// </param>
    /// <returns>A task that resolves to the result of starting the execution.</returns>
    Task<IBenzeneResult<TResponse>> StartExecutionAsync<TMessage, TResponse>(TMessage message, string executionName);
}
