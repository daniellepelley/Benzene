using Amazon.Lambda;
using Microsoft.Extensions.Logging;

namespace Benzene.Clients.Aws.Lambda;

/// <summary>
/// Creates <see cref="AwsLambdaBenzeneMessageClient"/> instances for a specific Lambda function.
/// </summary>
public class AwsLambdaBenzeneMessageClientFactory : IBenzeneMessageClientFactory
{
    private readonly string _lambdaName;
    private readonly ILogger<AwsLambdaBenzeneMessageClient> _logger;
    private readonly IAmazonLambda _amazonLambda;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsLambdaBenzeneMessageClientFactory"/> class.
    /// </summary>
    /// <param name="lambdaName">The name of the Lambda function clients created by this factory will target.</param>
    /// <param name="amazonLambda">The Lambda client used by created clients.</param>
    /// <param name="logger">The logger used by created clients.</param>
    public AwsLambdaBenzeneMessageClientFactory(string lambdaName, IAmazonLambda amazonLambda, ILogger<AwsLambdaBenzeneMessageClient> logger)
    {
        _amazonLambda = amazonLambda;
        _logger = logger;
        _lambdaName = lambdaName;
    }

    /// <summary>
    /// Creates a new <see cref="AwsLambdaBenzeneMessageClient"/> for the configured function.
    /// </summary>
    /// <returns>The created client.</returns>
    public IBenzeneMessageClient Create()
    {
        return new AwsLambdaBenzeneMessageClient(_lambdaName, _amazonLambda, _logger);
    }

    /// <summary>
    /// Creates a new client for the configured function, ignoring the given service and topic.
    /// </summary>
    /// <param name="service">Unused; this factory always targets the configured function.</param>
    /// <param name="topic">Unused; this factory always targets the configured function.</param>
    /// <returns>The created client.</returns>
    public IBenzeneMessageClient Create(string service, string topic)
    {
        return Create();
    }
}


