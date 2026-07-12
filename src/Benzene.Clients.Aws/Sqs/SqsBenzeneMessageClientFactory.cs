using Amazon.SQS;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Creates <see cref="SqsBenzeneMessageClient"/> instances for a specific queue.
/// </summary>
public class SqsBenzeneMessageClientFactory : IBenzeneMessageClientFactory
{
    private readonly ILogger<SqsBenzeneMessageClient> _logger;
    private readonly string _queueUrl;
    private readonly IAmazonSQS _amazonSqsClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsBenzeneMessageClientFactory"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue clients created by this factory will target.</param>
    /// <param name="amazonSqsClient">The SQS client used by created clients.</param>
    /// <param name="logger">The logger used by created clients.</param>
    public SqsBenzeneMessageClientFactory(string queueUrl, IAmazonSQS amazonSqsClient, ILogger<SqsBenzeneMessageClient> logger)
    {
        _amazonSqsClient = amazonSqsClient;
        _queueUrl = queueUrl;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new <see cref="SqsBenzeneMessageClient"/> for the configured queue.
    /// </summary>
    /// <returns>The created client.</returns>
    public virtual IBenzeneMessageClient Create()
    {
        return new SqsBenzeneMessageClient(_queueUrl, _amazonSqsClient, _logger, new NullServiceResolver());
    }

    /// <summary>
    /// Creates a new client for the configured queue, ignoring the given service and topic.
    /// </summary>
    /// <param name="service">Unused; this factory always targets the configured queue.</param>
    /// <param name="topic">Unused; this factory always targets the configured queue.</param>
    /// <returns>The created client.</returns>
    public IBenzeneMessageClient Create(string service, string topic)
    {
        return Create();
    }

}
