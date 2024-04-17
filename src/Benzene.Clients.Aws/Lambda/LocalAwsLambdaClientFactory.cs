using Amazon.Lambda;
using Amazon.Runtime.CredentialManagement;

namespace Benzene.Clients.Aws.Lambda
{
    public static class LocalAwsLambdaClientFactory
    {
        public static IAmazonLambda Create(string profileName)
        {
            var chain = new CredentialProfileStoreChain();
            return chain.TryGetAWSCredentials(profileName, out var awsCredentials)
                ? new AmazonLambdaClient(awsCredentials)
                : null;
        }
    }
}
