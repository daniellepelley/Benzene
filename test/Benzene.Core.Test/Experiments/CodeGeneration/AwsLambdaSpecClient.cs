using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda;
using Benzene.Clients.Aws.Lambda;
using Benzene.Schema.OpenApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Benzene.Test.Experiments.CodeGeneration;

public class AwsLambdaSpecClient
{
    private readonly IAwsLambdaClient _lambdaClient;
    private readonly ILogger _logger;
    private readonly string _lambdaName;

    public AwsLambdaSpecClient(string lambdaName, IAwsLambdaClient lambdaClient, ILogger logger)
    {
        _lambdaName = lambdaName;
        _logger = logger;
        _lambdaClient = lambdaClient;
    }

    public async Task<string> GetSpecAsync(SpecRequest specRequest)
    {
        try
        {
            var lambdaRequest = new BenzeneMessageClientRequest("spec", new Dictionary<string, string>(), JsonConvert.SerializeObject(specRequest));
            var response = await _lambdaClient.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(lambdaRequest, _lambdaName, InvocationType.RequestResponse);
            return response.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} to {receiver} failed", "spec", _lambdaName);
            return null;
        }
    }
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
