using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Results;

namespace Benzene.Examples.Aws.Tests.Helpers;

/// <summary>
/// Replaces the real <see cref="IBenzeneMessageSender"/> <c>DependenciesBuilder</c> wires onto a
/// real SQS outbound route, so the egress demo can be driven without a live queue - captures the
/// topic/request of the last call instead of sending it anywhere. Registered via
/// <c>BenzeneTestHostBuilder.WithServices</c>, which runs after the StartUp's own
/// <c>ConfigureServices</c> (last-registration-wins).
/// </summary>
public class FakeBenzeneMessageSender : IBenzeneMessageSender
{
    public string? LastTopic { get; private set; }
    public object? LastRequest { get; private set; }
    public IDictionary<string, string>? LastHeaders { get; private set; }

    public Task<IBenzeneResult<TResponse>> SendAsync<TRequest, TResponse>(string topic, TRequest request, IDictionary<string, string>? headers = null)
    {
        LastTopic = topic;
        LastRequest = request;
        LastHeaders = headers;
        return BenzeneResult.Accepted(default(TResponse)!).AsTask();
    }
}
