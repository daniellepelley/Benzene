using Amazon.Lambda;
using Benzene.Abstractions.Logging;

namespace Benzene.Clients.Aws.Lambda;

public class AwsLambdaBenzeneMessageClientFactory : IBenzeneMessageClientFactory
{
    private readonly string _lambdaName;
    private readonly IBenzeneLogger _logger;
    private readonly IAmazonLambda _amazonLambda;

    public AwsLambdaBenzeneMessageClientFactory(string lambdaName, IAmazonLambda amazonLambda, IBenzeneLogger logger)
    {
        _amazonLambda = amazonLambda;
        _logger = logger;
        _lambdaName = lambdaName;
    }

    public IBenzeneMessageClient Create()
    {
        return new AwsLambdaBenzeneMessageClient(_lambdaName, _amazonLambda, _logger);
    }

    public IBenzeneMessageClient Create(string service, string topic)
    {
        return Create();
    }
}


