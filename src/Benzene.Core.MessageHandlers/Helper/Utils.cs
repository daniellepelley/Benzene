using System.Reflection;

namespace Benzene.Core.MessageHandlers.Helper;

public static class Utils
{

    public static string GetValue(this IDictionary<string, string> dictionary, string key)
    {
        if (dictionary != null &&
            dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }

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
        catch
        {
            return Type.EmptyTypes;
        }
    }

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
