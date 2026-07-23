using Benzene.Aws.Lambda.Core;

namespace Benzene.Aws.Lambda.Core.TestHelpers;

public static class AwsLambdaBenzeneTestHostExtensions
{
    public static AwsLambdaBenzeneTestHost BuildHost(this IAwsEntryPointBuilder source)
    {
        return new AwsLambdaBenzeneTestHost(source.Build());
    }
}
