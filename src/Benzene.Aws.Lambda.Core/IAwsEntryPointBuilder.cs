namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Provides an abstraction for types that build an <see cref="IAwsLambdaEntryPoint"/>.
/// </summary>
/// <remarks>
/// Implemented by <see cref="InlineAwsLambdaStartUp"/> and by the AWS Lambda test host startup helpers,
/// which build an entry point without requiring a dedicated <c>StartUp</c> subclass.
/// </remarks>
public interface IAwsEntryPointBuilder
{
    /// <summary>
    /// Builds the configured Lambda entry point.
    /// </summary>
    /// <returns>The built <see cref="IAwsLambdaEntryPoint"/>, ready to handle invocations.</returns>
    IAwsLambdaEntryPoint Build();
}
