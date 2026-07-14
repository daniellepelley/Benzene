using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Testing;
using Benzene.Tools.Aws;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class LambdaEntryPointFactory
{
    public static IAwsLambdaEntryPoint Create()
    {
        return BenzeneTestHost.Create<StartUp>().BuildAwsLambdaHost();
    }
}