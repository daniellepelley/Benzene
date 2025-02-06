namespace Benzene.Aws.Lambda.Core;

public interface IAwsEntryPointBuilder
{
    IAwsLambdaEntryPoint Build();
}