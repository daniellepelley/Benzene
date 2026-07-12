using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;

namespace Benzene.Clients.Aws.Sqs
{
    /// <summary>
    /// Creates an <see cref="IAmazonSQS"/> client from a local AWS credentials profile, for local
    /// development and testing.
    /// </summary>
    public static class LocalSqsClientFactory
    {
        /// <summary>
        /// Creates an SQS client using credentials from the given local AWS profile.
        /// </summary>
        /// <param name="profileName">The name of the local AWS credentials profile to use.</param>
        /// <returns>The created SQS client, or null if the profile could not be found.</returns>
        public static IAmazonSQS Create(string profileName)
        {
            var chain = new CredentialProfileStoreChain();
            return chain.TryGetAWSCredentials(profileName, out var awsCredentials)
                ? new AmazonSQSClient(awsCredentials)
                : null;
        }
    }
}
