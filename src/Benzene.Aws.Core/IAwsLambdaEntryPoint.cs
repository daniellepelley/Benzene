using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Benzene.Aws.Core;

public interface IAwsLambdaEntryPoint : IDisposable
{
    Task<Stream> FunctionHandler(Stream stream, ILambdaContext lambdaContext);
}