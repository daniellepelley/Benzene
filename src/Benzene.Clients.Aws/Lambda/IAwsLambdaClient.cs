using System.Threading.Tasks;
using Amazon.Lambda;

namespace Benzene.Clients.Aws.Lambda
{
    public interface IAwsLambdaClient
    {
        Task<TResponse> SendMessageAsync<TRequest, TResponse>(TRequest request, string functionName, InvocationType invocationType);
    }
}
