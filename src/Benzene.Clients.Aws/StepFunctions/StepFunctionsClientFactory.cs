using Amazon.StepFunctions;
using Benzene.Abstractions.Logging;

namespace Benzene.Clients.Aws.StepFunctions;

public interface IStepFunctionsClientFactory
{
    IStepFunctionsClient Create();
}

public class StepFunctionsClientFactory : IStepFunctionsClientFactory
{
    private readonly IBenzeneLogger _logger;
    private readonly string _stateMachineArn;
    private readonly IAmazonStepFunctions _amazonStepFunctionsClient;

    public StepFunctionsClientFactory(string stateMachineArn, IAmazonStepFunctions amazonStepFunctionsClient, IBenzeneLogger logger)
    {
        _amazonStepFunctionsClient = amazonStepFunctionsClient;
        _stateMachineArn = stateMachineArn;
        _logger = logger;
    }

    public virtual IStepFunctionsClient Create()
    {
        return new StepFunctionsClient(_stateMachineArn, _amazonStepFunctionsClient, _logger);
    }
}
