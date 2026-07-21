using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Benzene.Core.Helper;

/// <summary>
/// Provides general utility methods for common operations.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Gets a value from a dictionary by key, returning null if the key is not found.
    /// </summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value associated with the key, or null if the key is not found.</returns>
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
    /// Gets all types from the specified assemblies, excluding nested private types and dynamic assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan. If null or empty, scans all assemblies in the current domain.</param>
    /// <returns>A collection of all types found in the assemblies.</returns>
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
            // Keep the types that DID load rather than dropping the whole assembly. One unloadable
            // type (e.g. a missing optional dependency) would otherwise make every handler in the
            // assembly undiscoverable - a silent 404. Mirrors ValidateOutboundRoutingExtensions.
            return ex.Types.OfType<Type>().ToArray();
        }
        catch
        {
            return Type.EmptyTypes;
        }
    }

    /// <summary>
    /// Gets assemblies, filtering out dynamic assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to process. If null or empty, returns all assemblies in the current domain.</param>
    /// <returns>A collection of non-dynamic assemblies.</returns>
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
