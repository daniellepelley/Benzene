using System.Reflection;
using System.Runtime.Loader;

namespace Benzene.Descriptor;

/// <summary>
/// Loads a built service assembly and its service-unique dependencies (FluentValidation, the AWS SDK,
/// whichever Benzene transport packages it uses, …) from the service's own output folder, while
/// deferring every assembly the tool already carries to the default context. Deferring the shared
/// Benzene/Microsoft/System contract assemblies is what keeps type identity intact across the
/// boundary — e.g. the service's <c>StartUp</c> stays assignable to the tool's <c>BenzeneStartUp</c>.
///
/// This is the standard .NET "plugin" ALC pattern (an <see cref="AssemblyDependencyResolver"/> seeded
/// from the service's <c>.deps.json</c>), and it assumes the tool and the service resolve the shared
/// Benzene.* assemblies to the same version — pin the tool to the service's Benzene version.
/// </summary>
internal sealed class ServiceLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public ServiceLoadContext(string serviceAssemblyPath)
        : base(name: "benzene-descriptor-service", isCollectible: false)
    {
        _resolver = new AssemblyDependencyResolver(serviceAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Prefer the tool's already-referenced copy (the default context) for anything it can supply —
        // this unifies the shared contract types. Only fall through to the service's own folder for
        // assemblies the tool doesn't carry (the service's unique transports/deps).
        try
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }
        catch (FileNotFoundException)
        {
            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path != null ? LoadFromAssemblyPath(path) : null;
        }
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
