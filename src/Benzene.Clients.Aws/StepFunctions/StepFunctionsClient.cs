using System;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Clients.Aws.StepFunctions;

public class StepFunctionsClient : IStepFunctionsClient
{
    private readonly IBenzeneLogger _logger;
    private readonly string _stateMachineArn;
    private readonly IAmazonStepFunctions _amazonStepFunctionsClient;
    private readonly ISerializer _serializer;

    public StepFunctionsClient(string stateMachineArn, IAmazonStepFunctions amazonStepFunctionsClient, IBenzeneLogger logger)
    {
        _amazonStepFunctionsClient = amazonStepFunctionsClient;
        _logger = logger;
        _stateMachineArn = stateMachineArn;
        _serializer = new JsonSerializer();
    }

    public async Task<IBenzeneResult<TResponse>> StartExecutionAsync<TMessage, TResponse>(TMessage message)
    {
        try
        {
            await _amazonStepFunctionsClient.StartExecutionAsync(new StartExecutionRequest
            {
                StateMachineArn = _stateMachineArn,
                Input = _serializer.Serialize(message)
            });

            return BenzeneResult.Accepted<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message to {receiver} failed", _stateMachineArn);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }
        
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
