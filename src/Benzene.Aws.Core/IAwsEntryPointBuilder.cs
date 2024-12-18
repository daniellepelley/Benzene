namespace Benzene.Aws.Core;

public interface IAwsEntryPointBuilder
{
    IAwsLambdaEntryPoint Build();
}