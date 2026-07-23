using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Testing;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class LambdaEntryPointFactory
{
    public static IAwsLambdaEntryPoint Create()
    {
        return BenzeneTestHost.Create<StartUp>().BuildAwsLambdaHost();
    }
}
