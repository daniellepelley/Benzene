using System.Diagnostics;

namespace Benzene.Examples.OpenTelemetry;

public static class ExampleDiagnostics
{
    public const string SourceName = "Benzene.Examples.OpenTelemetry";
    public static readonly ActivitySource ActivitySource = new(SourceName);
}
