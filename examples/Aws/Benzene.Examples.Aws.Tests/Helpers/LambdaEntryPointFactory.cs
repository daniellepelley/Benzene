using Benzene.Aws.Lambda.Core;
using Benzene.Tools.Aws;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class LambdaEntryPointFactory
{
    public static IAwsLambdaEntryPoint Create()
    {
        return new AwsLambdaBenzeneTestStartUp<StartUp>().Build();
    }
}