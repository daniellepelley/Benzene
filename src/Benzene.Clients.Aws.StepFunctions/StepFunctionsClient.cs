using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;
using Microsoft.Extensions.Logging;

namespace Benzene.Clients.Aws.StepFunctions;

/// <summary>
/// Starts executions of an AWS Step Functions state machine.
/// </summary>
public class StepFunctionsClient : IStepFunctionsClient
{
    private readonly ILogger<StepFunctionsClient> _logger;
    private readonly string _stateMachineArn;
    private readonly IAmazonStepFunctions _amazonStepFunctionsClient;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepFunctionsClient"/> class.
    /// </summary>
    /// <param name="stateMachineArn">The ARN of the state machine to start executions on.</param>
    /// <param name="amazonStepFunctionsClient">The Step Functions client used to start executions.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    public StepFunctionsClient(string stateMachineArn, IAmazonStepFunctions amazonStepFunctionsClient, ILogger<StepFunctionsClient> logger)
    {
        _amazonStepFunctionsClient = amazonStepFunctionsClient;
        _logger = logger;
        _stateMachineArn = stateMachineArn;
        _serializer = new JsonSerializer();
    }

    /// <summary>
    /// Starts a new execution of the state machine with the given message as its input.
    /// </summary>
    /// <typeparam name="TMessage">The type of the input message.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="message">The message to serialize as the execution input.</param>
    /// <returns>
    /// A task that resolves to an accepted result if the execution started successfully, or a
    /// service-unavailable result if starting it threw.
    /// </returns>
    public Task<IBenzeneResult<TResponse>> StartExecutionAsync<TMessage, TResponse>(TMessage message)
    {
        return StartExecutionAsync<TMessage, TResponse>(message, executionName: null);
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult<TResponse>> StartExecutionAsync<TMessage, TResponse>(TMessage message, string executionName)
    {
        var name = SanitizeExecutionName(executionName);

        try
        {
            await _amazonStepFunctionsClient.StartExecutionAsync(new StartExecutionRequest
            {
                StateMachineArn = _stateMachineArn,
                Input = _serializer.Serialize(message),
                // Null Name lets AWS generate a UUID (the original behavior); a supplied name makes the
                // start idempotent for the same (state machine, name, input).
                Name = name
            });

            return BenzeneResult.Accepted<TResponse>();
        }
        catch (ExecutionAlreadyExistsException)
        {
            // The idempotency name was already used: a prior attempt (e.g. before a lost response)
            // already started this execution. Treat the retry as success rather than a failure.
            return BenzeneResult.Accepted<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message to {receiver} failed", _stateMachineArn);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    /// <summary>
    /// Sanitizes an idempotency token into a valid Step Functions execution name: Step Functions
    /// rejects whitespace, control characters, and the set <c>&lt; &gt; { } [ ] ? * " # % \ ^ | ~ ` $ &amp; , ; : /</c>,
    /// and caps the name at 80 characters. Disallowed characters are replaced with <c>-</c>. Returns
    /// <c>null</c> for a null/empty token so AWS generates a UUID name.
    /// </summary>
    private static string SanitizeExecutionName(string executionName)
    {
        if (string.IsNullOrEmpty(executionName))
        {
            return null;
        }

        var builder = new StringBuilder(executionName.Length);
        foreach (var c in executionName)
        {
            var allowed = !char.IsWhiteSpace(c) && !char.IsControl(c) &&
                          "<>{}[]?*\"#%\\^|~`$&,;:/".IndexOf(c) < 0;
            builder.Append(allowed ? c : '-');
        }

        var sanitized = builder.ToString();
        return sanitized.Length > 80 ? sanitized.Substring(0, 80) : sanitized;
    }

    /// <summary>
    /// Disposes the client. No-op; the client holds no disposable resources of its own.
    /// </summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
