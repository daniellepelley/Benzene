using System;

namespace Benzene.Mesh.Dispatch;

/// <summary>Tells the dispatch gate whether the process is running in a Production environment.</summary>
public interface IMeshDispatchEnvironment
{
    /// <summary>Whether this process considers itself to be running in Production.</summary>
    bool IsProduction { get; }
}

/// <summary>
/// Default <see cref="IMeshDispatchEnvironment"/> reading the standard ASP.NET Core environment
/// variables (<c>ASPNETCORE_ENVIRONMENT</c> / <c>DOTNET_ENVIRONMENT</c>). An UNSET environment is
/// treated as Production - the safe default, so dispatch is off unless the host is explicitly a
/// non-production environment (or <see cref="MeshDispatchOptions.AllowInProduction"/> is set).
/// </summary>
public class EnvironmentVariableMeshDispatchEnvironment : IMeshDispatchEnvironment
{
    /// <inheritdoc />
    public bool IsProduction
    {
        get
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            return string.IsNullOrWhiteSpace(environment)
                || string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
        }
    }
}
