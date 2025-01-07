using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;

namespace Benzene.Integration.Test.Fixtures;

public class DockerComposeFixture : IDisposable
{
    private static ICompositeService? _compositeService;
    private readonly string _fileName;

    public DockerComposeFixture(string fileName)
    {
        _fileName = $"Fixtures/Files/{fileName}";
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
