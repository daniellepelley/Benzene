using Amazon.Lambda;
using Amazon.Runtime.CredentialManagement;

namespace Benzene.Clients.Aws.Lambda
{
    /// <summary>
    /// Creates an <see cref="IAmazonLambda"/> client from a local AWS credentials profile, for local
    /// development and testing.
    /// </summary>
    public static class LocalAwsLambdaClientFactory
    {
        /// <summary>
        /// Creates a Lambda client using credentials from the given local AWS profile.
        /// </summary>
        /// <param name="profileName">The name of the local AWS credentials profile to use.</param>
        /// <returns>The created Lambda client, or null if the profile could not be found.</returns>
        public static IAmazonLambda Create(string profileName)
        {
            var chain = new CredentialProfileStoreChain();
            return chain.TryGetAWSCredentials(profileName, out var awsCredentials)
                ? new AmazonLambdaClient(awsCredentials)
                : null;
        }
    }
}
