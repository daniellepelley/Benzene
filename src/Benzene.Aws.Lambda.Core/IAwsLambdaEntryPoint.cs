using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Represents the entry point AWS Lambda invokes for each function invocation.
/// </summary>
/// <remarks>
/// Implemented by <see cref="AwsLambdaEntryPoint"/>, which is hosted by
/// <see cref="AwsLambdaHost{TStartUp}"/> for the platform-neutral <c>BenzeneStartUp</c> model.
/// </remarks>
public interface IAwsLambdaEntryPoint : IDisposable
{
    /// <summary>
    /// Handles a single AWS Lambda invocation.
    /// </summary>
    /// <param name="stream">The raw Lambda invocation payload stream.</param>
    /// <param name="lambdaContext">The AWS Lambda execution context for this invocation.</param>
    /// <returns>A task that resolves to the response stream to return from the invocation.</returns>
    Task<Stream> FunctionHandlerAsync(Stream stream, ILambdaContext lambdaContext);
}
