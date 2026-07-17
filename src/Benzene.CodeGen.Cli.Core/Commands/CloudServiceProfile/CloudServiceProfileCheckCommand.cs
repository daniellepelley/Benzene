using Benzene.CloudService.Probe;

namespace Benzene.CodeGen.Cli.Core.Commands.CloudServiceProfile;

/// <summary>
/// CLI wrapper for the external live-probe checker (docs/specification/cloud-service-profile.md
/// §5, <c>Benzene.CloudService.Probe</c>): hits a running Benzene Cloud Service over plain HTTP and
/// independently assesses R1-R8 without trusting anything the service claims about itself. Unlike
/// <see cref="Commands.HealthCheck.HealthCheckCommand"/>/<see cref="Commands.Spec.SpecCommand"/>,
/// this talks to a plain HTTP URL rather than invoking AWS Lambda directly.
/// </summary>
public class CloudServiceProfileCheckCommand : CommandBase<CloudServiceProfileCheckPayload>
{
    public CloudServiceProfileCheckCommand()
        : base("profile-check", "Externally probes a Benzene Cloud Service's R1-R8 conformance over HTTP")
    { }

    public override async Task ExecuteAsync(CloudServiceProfileCheckPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Url))
        {
            Console.Error.WriteLine("--url is required: the base URL of the Benzene Cloud Service to probe");
            return;
        }

        var options = new CloudServiceProbeOptions
        {
            SendTraceParentProbe = !string.Equals(payload.NoTraceParentProbe, "true", StringComparison.OrdinalIgnoreCase)
        };
        if (!string.IsNullOrWhiteSpace(payload.InvokePath))
        {
            options.InvokePath = payload.InvokePath;
        }
        if (!string.IsNullOrWhiteSpace(payload.SpecPath))
        {
            options.SpecPath = payload.SpecPath;
        }
        if (!string.IsNullOrWhiteSpace(payload.HealthPath))
        {
            options.HealthPath = payload.HealthPath;
        }

        using var httpClient = new HttpClient { BaseAddress = new Uri(payload.Url) };
        var report = await CloudServiceProbe.RunAsync(httpClient, options);

        Console.WriteLine($"Cloud Service Profile probe: {payload.Url}");
        Console.WriteLine();
        foreach (var requirement in report.Requirements)
        {
            Console.WriteLine($"{requirement.Id,-4} {requirement.Verdict,-13} {requirement.Description}");
            Console.WriteLine($"     {requirement.Reason}");
        }

        var satisfiedCount = report.Requirements.Count(x => x.Verdict == CloudServiceProbeVerdict.Satisfied);
        var notSatisfied = report.NotSatisfied;
        var inconclusive = report.Inconclusive;
        Console.WriteLine();
        Console.WriteLine(
            $"Summary: {satisfiedCount}/{report.Requirements.Count} satisfied" +
            $", {notSatisfied.Count} not satisfied" + (notSatisfied.Count == 0 ? "" : $" [{string.Join(", ", notSatisfied)}]") +
            $", {inconclusive.Count} inconclusive" + (inconclusive.Count == 0 ? "" : $" [{string.Join(", ", inconclusive)}]"));
    }
}
