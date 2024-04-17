using Amazon.Lambda;
using Benzene.Clients.Aws.Lambda;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Benzene.CodeGen.Cli.Core.Commands.HealthCheck;

public class HealthCheckClient
{
    private readonly IAwsLambdaClient _lambdaClient;
    private readonly ILogger _logger;
    private readonly string _lambdaName;

    public HealthCheckClient(string lambdaName, IAwsLambdaClient lambdaClient, ILogger logger)
    {
        _lambdaName = lambdaName;
        _logger = logger;
        _lambdaClient = lambdaClient;
    }

    public async Task<string> GetHealthCheckAsync()
    {
        try
        {
            var lambdaRequest = new BenzeneMessageClientRequest("healthcheck", new Dictionary<string, string>(), JsonConvert.SerializeObject(new object()));
            var response = await _lambdaClient.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(lambdaRequest, _lambdaName, InvocationType.RequestResponse);
            return response.Message;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return null;
        }
    }
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
