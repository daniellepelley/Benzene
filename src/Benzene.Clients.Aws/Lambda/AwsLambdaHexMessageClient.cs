using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Serialization;
using Benzene.Clients.Aws.Common;
using Benzene.Results;

namespace Benzene.Clients.Aws.Lambda
{
    public class AwsLambdaBenzeneMessageClient : IBenzeneMessageClient
    {
        private readonly string _lambdaName;
        private readonly IBenzeneLogger _logger;
        private readonly AwsLambdaClient _awsLambdaClient;
        private readonly ISerializer _serializer;

        public AwsLambdaBenzeneMessageClient(string lambdaName, IAmazonLambda amazonLambda, IBenzeneLogger logger)
        {
            _awsLambdaClient = new AwsLambdaClient(amazonLambda);
            _lambdaName = lambdaName;
            _logger = logger;
            _serializer = new JsonSerializer();
        }
        public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
        {
            try
            {
                var lambdaRequest = new BenzeneMessageClientRequest(request.Topic, request.Headers, _serializer.Serialize(request.Message));

                var invocationType = typeof(TResponse).Name == "Void"
                    ? InvocationType.Event
                    : InvocationType.RequestResponse;

                if (invocationType == InvocationType.Event)
                {
                    _logger.LogInformation("Fire and Forget message {receiverTopic} sent to {receiver}", request.Topic, _lambdaName);
                    await _awsLambdaClient.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(lambdaRequest, _lambdaName, invocationType);
                    return BenzeneResult.Accepted<TResponse>();
                }

                var response = await _awsLambdaClient.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(lambdaRequest, _lambdaName, invocationType);
                var clientResult = response.AsBenzeneResult<TResponse>(_serializer);

                _logger.LogInformation("Request and Response message {receiverTopic} sent to {receiver} with status {receiverStatus}",
                    request.Topic, _lambdaName, clientResult.Status);

                return clientResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sending message {receiverTopic} to {receiver} failed", request.Topic, _lambdaName);
                return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
            }
        }

        public void Dispose()
        {
            // Method intentionally left empty.
        }

    }
}
