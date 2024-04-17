using System;
using System.Threading.Tasks;
using Benzene.Results;

namespace Benzene.Clients.Aws.StepFunctions;

public interface IStepFunctionsClient : IDisposable
{
    Task<IClientResult<TResponse>> StartExecutionAsync<TMessage, TResponse>(TMessage message);
}
