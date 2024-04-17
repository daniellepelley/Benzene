using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;

namespace Benzene.Clients.Aws.Sqs
{
    public static class LocalSqsClientFactory
    {
        public static IAmazonSQS Create(string profileName)
        {
            var chain = new CredentialProfileStoreChain();
            return chain.TryGetAWSCredentials(profileName, out var awsCredentials)
                ? new AmazonSQSClient(awsCredentials)
                : null;
        }
    }
}
