using System;
using System.Collections.Generic;
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

        public async Task<IClientResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers)
        {
            try
            {
                var lambdaRequest = new BenzeneMessageClientRequest(topic, headers, _serializer.Serialize(message));

                var invocationType = typeof(TResponse).Name == "Void"
                    ? InvocationType.Event
                    : InvocationType.RequestResponse;

                if (invocationType == InvocationType.Event)
                {
                    _logger.LogInformation("Fire and Forget message {receiverTopic} sent to {receiver}", topic, _lambdaName);
                    await _awsLambdaClient.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(lambdaRequest, _lambdaName, invocationType);
                    return ClientResult.Accepted<TResponse>();
                }

                var response = await _awsLambdaClient.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(lambdaRequest, _lambdaName, invocationType);
                var clientResult = response.AsClientResult<TResponse>(_serializer);

                _logger.LogInformation("Request and Response message {receiverTopic} sent to {receiver} with status {receiverStatus}",
                    topic, _lambdaName, clientResult.Status);

                return clientResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sending message {receiverTopic} to {receiver} failed", topic, _lambdaName);
                return ClientResult.ServiceUnavailable<TResponse>(ex.Message);
            }
        }

        public void Dispose()
        {
            // Method intentionally left empty.
        }
    }
}
