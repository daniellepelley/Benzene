using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Clients.Aws.StepFunctions;

public interface IStepFunctionsClient : IDisposable
{
    Task<IBenzeneResult<TResponse>> StartExecutionAsync<TMessage, TResponse>(TMessage message);
}
