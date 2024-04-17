using Amazon.Lambda;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace Benzene.CodeGen.Cli.Core.Commands.Build;

public static class AmazonLambdaClientFactory
{
    public static AmazonLambdaClient CreateClient(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
        {
            return new AmazonLambdaClient();
        }

        var chain = new CredentialProfileStoreChain();
        AWSCredentials awsCredentials;
        if (chain.TryGetAWSCredentials(profileName, out awsCredentials))
        {
            // Use awsCredentials to create an Amazon S3 service client
            return new AmazonLambdaClient(awsCredentials);
        }

        return null;
    }
}