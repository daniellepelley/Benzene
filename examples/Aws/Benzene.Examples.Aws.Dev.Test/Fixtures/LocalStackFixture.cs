using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;

namespace Benzene.Examples.Aws.Dev.Test.Fixtures;

/// <summary>
/// Starts a LocalStack container via <c>Ductus.FluentDocker</c> (which drives Docker Compose
/// programmatically from this process rather than needing a separate shell/CI step) and waits for
/// its health endpoint before returning. Mirrors the library-level
/// <c>test/Benzene.Aws.Tests/Fixtures/LocalStackFixture</c> - kept as a separate copy so the example
/// tier stays self-contained and doesn't reach into the library test project.
/// </summary>
public class LocalStackFixture : IDisposable
{
    private const string HealthCheckUrl = "http://localhost:4566/_localstack/health";
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly ICompositeService _compositeService;

    public LocalStackFixture()
    {
        _compositeService = StartLocalStack("Fixtures/Files/sqs-docker-compose.yaml");
        WaitUntilReady();
    }

    public void Dispose()
    {
        _compositeService.Dispose();
        GC.SuppressFinalize(this);
    }

    private static ICompositeService StartLocalStack(string fileName)
    {
        return new Builder()
            .UseContainer()
            .UseCompose()
            .FromFile(Path.Combine(Directory.GetCurrentDirectory(), (TemplateString)fileName))
            .ForceBuild()
            .Build()
            .Start();
    }

    private static void WaitUntilReady()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + ReadyTimeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = httpClient.GetAsync(HealthCheckUrl).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // LocalStack isn't accepting connections yet - keep polling until the deadline.
            }

            Thread.Sleep(PollInterval);
        }

        throw new TimeoutException($"LocalStack did not become ready at {HealthCheckUrl} within {ReadyTimeout}.");
    }
}
