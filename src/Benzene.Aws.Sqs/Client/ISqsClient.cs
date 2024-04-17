using System.Threading.Tasks;

namespace Benzene.Aws.Sqs.Client;

public interface ISqsClient
{
    Task<string> PublishAsync(string topic, string message, string status);
}
