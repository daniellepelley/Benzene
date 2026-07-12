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
}
