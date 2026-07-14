using System.Text.Json;
using Benzene.Conformance.Test.Handlers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Conformance.Test;

/// <summary>
/// Runs docs/specification/conformance/envelope-cases.json through the real BenzeneMessage pipeline
/// with the canonical conformance handlers registered - the .NET reference runner for the envelope
/// contract (wire-contracts.md section 1).
/// </summary>
public class EnvelopeConformanceTest
{
    public class EnvelopeFixture
    {
        public List<EnvelopeCase> Cases { get; set; } = new();
    }

    public class EnvelopeCase
    {
        public string Name { get; set; } = string.Empty;
        public EnvelopeRequest Request { get; set; } = new();
        public EnvelopeExpectation Expected { get; set; } = new();
    }

    public class EnvelopeRequest
    {
        public string Topic { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;
    }

    public class EnvelopeExpectation
    {
        public string StatusCode { get; set; } = string.Empty;
        public JsonElement? Body { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }

    private static readonly Lazy<EnvelopeFixture> Fixture = new(() =>
        ConformanceFixtures.Load<EnvelopeFixture>("envelope-cases.json"));

    public static IEnumerable<object[]> CaseNames()
    {
        return Fixture.Value.Cases.Select(x => new object[] { x.Name });
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task EnvelopeCase_ProducesTheExpectedResponse(string caseName)
    {
        var envelopeCase = Fixture.Value.Cases.Single(x => x.Name == caseName);

        var response = await RunPipelineAsync(new BenzeneMessageRequest
        {
            Topic = envelopeCase.Request.Topic,
            Headers = envelopeCase.Request.Headers,
            Body = envelopeCase.Request.Body
        });

        Assert.NotNull(response);
        Assert.Equal(envelopeCase.Expected.StatusCode, response.StatusCode);

        if (envelopeCase.Expected.Body is { } expectedBody)
        {
            Assert.False(string.IsNullOrEmpty(response.Body), $"{caseName}: expected a response body but none was written");
            using var actualBody = JsonDocument.Parse(response.Body);
            var mismatch = ConformanceFixtures.FindSubsetMismatch(expectedBody, actualBody.RootElement);
            Assert.True(mismatch == null, $"{caseName}: body mismatch at {mismatch}");
        }

        if (envelopeCase.Expected.Headers != null)
        {
            foreach (var header in envelopeCase.Expected.Headers)
            {
                var actualValue = response.Headers?
                    .Where(x => string.Equals(x.Key, header.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Value)
                    .FirstOrDefault();
                Assert.True(header.Value == actualValue,
                    $"{caseName}: header '{header.Key}' expected '{header.Value}' but found '{actualValue ?? "<missing>"}'");
            }
        }
    }

    private static async Task<IBenzeneMessageResponse> RunPipelineAsync(BenzeneMessageRequest request)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage();

        var pipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipelineBuilder.UseMessageHandlers(typeof(GreetConformanceHandler), typeof(StatusConformanceHandler));
        var pipeline = pipelineBuilder.Build();

        var application = new BenzeneMessageApplication(pipeline);
        return await application.HandleAsync(request, container.CreateServiceResolverFactory());
    }
}
