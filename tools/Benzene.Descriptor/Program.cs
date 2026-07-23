using Benzene.Descriptor;

// benzene-descriptor --assembly <path-to-service.dll> [options]
//
// Emits a deployment descriptor (service.json) from a built, non-running Benzene AWS Lambda service.
// Intended to run as a post-build step (see build/Benzene.Descriptor.targets).

var opts = EmitOptions.Parse(args);
if (opts is null)
{
    Console.Error.WriteLine(EmitOptions.Usage);
    return 2;
}

try
{
    var json = DescriptorEmitter.Emit(opts);
    if (opts.OutputPath is null)
    {
        Console.WriteLine(json);
    }
    else
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(opts.OutputPath))!);
        File.WriteAllText(opts.OutputPath, json);
        Console.WriteLine($"benzene-descriptor: wrote {opts.OutputPath}");
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"benzene-descriptor: {ex.Message}");
    return 1;
}

namespace Benzene.Descriptor
{
    internal sealed class EmitOptions
    {
        public const string Usage =
            "Usage: benzene-descriptor --assembly <service.dll> [--output <service.json>] " +
            "[--service <name>] [--service-version <v>] [--cloud <aws>] [--region <r>]";

        public required string AssemblyPath { get; init; }
        public string? OutputPath { get; init; }
        public required string ServiceName { get; init; }
        public string? ServiceVersion { get; init; }
        public string Cloud { get; init; } = "aws";
        public string Region { get; init; } = "eu-west-1";
        // Force a specific host adapter (e.g. "neutral" for the cloud-agnostic core); auto-selected if null.
        public string? Host { get; init; }

        public static EmitOptions? Parse(string[] args)
        {
            string? assembly = null, output = null, service = null, version = null, cloud = null, region = null, host = null;
            for (var i = 0; i < args.Length; i++)
            {
                string? Next() => i + 1 < args.Length ? args[++i] : null;
                switch (args[i])
                {
                    case "--assembly": assembly = Next(); break;
                    case "--output": output = Next(); break;
                    case "--service": service = Next(); break;
                    case "--service-version": version = Next(); break;
                    case "--cloud": cloud = Next(); break;
                    case "--region": region = Next(); break;
                    case "--host": host = Next(); break;
                    default: return null;
                }
            }

            if (string.IsNullOrWhiteSpace(assembly)) return null;

            return new EmitOptions
            {
                AssemblyPath = assembly,
                OutputPath = output,
                // Default the service name to the assembly's simple name if not supplied.
                ServiceName = service ?? Path.GetFileNameWithoutExtension(assembly),
                ServiceVersion = version,
                Cloud = cloud ?? "aws",
                Region = region ?? "eu-west-1",
                Host = host,
            };
        }
    }
}
