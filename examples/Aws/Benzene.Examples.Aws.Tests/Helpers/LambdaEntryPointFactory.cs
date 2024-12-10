using Benzene.Aws.Core;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class LambdaEntryPointFactory
{
    public static IAwsLambdaEntryPoint Create()
    {
        return new TestLambdaStartUp<StartUp>().Build();
    }
}