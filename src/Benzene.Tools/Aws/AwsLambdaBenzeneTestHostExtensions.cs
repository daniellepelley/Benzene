using Benzene.Aws.Lambda.Core;

namespace Benzene.Tools.Aws;

public static class AwsLambdaBenzeneTestHostExtensions
{
    public static AwsLambdaBenzeneTestHost BuildHost(this IAwsEntryPointBuilder source)
    {
        return new AwsLambdaBenzeneTestHost(source.Build());
    }
}
