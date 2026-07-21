using System.Reflection;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Small reflection and dictionary helpers used throughout handler discovery and request enrichment.
/// </summary>
/// <remarks>
/// A near-identical set of helpers also exists as <see cref="Benzene.Core.MessageHandlers.Helper.Utils"/>
/// in the <c>Helper</c> namespace; both are kept as they are used by code that was written against
/// each namespace independently.
/// </remarks>
public static class Utils
{

    /// <summary>
    /// Gets a value from a dictionary by key, returning <c>null</c> instead of throwing if the
    /// dictionary is <c>null</c> or does not contain the key.
    /// </summary>
    /// <param name="dictionary">The dictionary to look up.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value for <paramref name="key"/>, or <c>null</c> if not found.</returns>
    public static string GetValue(this IDictionary<string, string> dictionary, string key)
    {
        if (dictionary != null &&
            dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Gets every non-nested-private type across the given assemblies (or every non-dynamic loaded
    /// assembly if none are supplied).
    /// </summary>
    /// <param name="assemblies">The assemblies to scan, or none to scan every currently loaded assembly.</param>
    /// <returns>The candidate types, e.g. for handler or filter discovery.</returns>
    public static IEnumerable<Type> GetAllTypes(params Assembly[] assemblies)
    {
        return GetAssemblies(assemblies)
            .SelectMany(GetTypes)
            .Where(x => !x.IsNestedPrivate);
    }

    private static Type[] GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Recover the types that DID load. Dropping the whole assembly here made every handler
            // in a partially-loadable assembly silently undiscoverable - a 404 with no explanation.
            // Mirrors ValidateOutboundRoutingExtensions' handling of the same exception.
            return ex.Types.Where(t => t != null).ToArray()!;
        }
        catch
        {
            return Type.EmptyTypes;
        }
    }

    /// <summary>
    /// Gets the given assemblies, excluding dynamic ones, or every non-dynamic loaded assembly if none are supplied.
    /// </summary>
    /// <param name="assemblies">The assemblies to filter, or none to use every currently loaded assembly.</param>
    /// <returns>The non-dynamic assemblies to scan.</returns>
    public static IEnumerable<Assembly> GetAssemblies(params Assembly[] assemblies)
    {
        return GetAssembliesCollection(assemblies)
            .Where(x => !x.IsDynamic);
    }

    private static IEnumerable<Assembly> GetAssembliesCollection(params Assembly[] assemblies)
    {
        return assemblies != null && assemblies.Any()
            ? assemblies
            : AppDomain.CurrentDomain.GetAssemblies();
    }

}
