using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;

namespace Benzene.Integration.Test.Fixtures;

/// <summary>
/// Base class for xunit fixtures that bring up a Docker Compose stack for the duration of a test
/// run. The compose file passed to the constructor should live in its own subdirectory under
/// <c>Fixtures/Files/</c> (e.g. <c>"Sqs/sqs-docker-compose.yaml"</c>), one per fixture - not
/// alongside another compose file. Docker Compose derives its default project name from the
/// compose file's containing directory when none is set explicitly, and this project has no way to
/// override that via FluentDocker's fluent Builder API; two compose files sharing a directory (and
/// therefore a default project name) can silently step on each other's containers/networks,
/// especially if they happen to reuse the same service name (as this project's Event Hub and
/// Service Bus compose files both did before this was discovered and fixed).
/// </summary>
public class DockerComposeFixture : IDisposable
{
    private ICompositeService? _compositeService;
    private readonly string _fileName;

    public DockerComposeFixture(string fileName)
    {
        _fileName = Path.Combine("Fixtures","Files", fileName);
        DockerComposeUp();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void DockerComposeUp()
    {
        var builder = new Builder()
            .UseContainer()
            .UseCompose()
            .FromFile(Path.Combine(Directory.GetCurrentDirectory(), (TemplateString)_fileName))
            .ForceBuild();

        _compositeService = builder
            .Build()
            .Start();

        var state = _compositeService.State;
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_compositeService != null)
            {
                _compositeService.Dispose();
            }
        }
    }
}
