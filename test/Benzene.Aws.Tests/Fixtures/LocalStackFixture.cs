using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;

namespace Benzene.Aws.Tests.Fixtures;

/// <summary>
/// Starts a LocalStack container (and any sibling services declared in the same compose
/// file) via <c>Ductus.FluentDocker</c>, which drives Docker Compose programmatically from
/// this process rather than requiring a separate shell/CI step. Waits for LocalStack's
/// health endpoint to respond before returning, since the edge server isn't immediately
/// ready the instant the container starts.
/// </summary>
public class LocalStackFixture : IDisposable
{
    private const string HealthCheckUrl = "http://localhost:4566/_localstack/health";
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly ICompositeService _compositeService;

    public LocalStackFixture(string fileName)
    {
        _compositeService = StartLocalStack($"Fixtures/Files/{fileName}");
        WaitUntilReady();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static ICompositeService StartLocalStack(string fileName)
    {
        var builder = new Builder()
            .UseContainer()
            .UseCompose()
            .FromFile(Path.Combine(Directory.GetCurrentDirectory(), (TemplateString)fileName))
            .ForceBuild();

        return builder
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

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _compositeService.Dispose();
        }
    }
}
