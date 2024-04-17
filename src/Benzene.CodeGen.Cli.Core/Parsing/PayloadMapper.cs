using System.Reflection;

namespace Benzene.CodeGen.Cli.Core.Parsing;

public static class PayloadMapper
{
    public static T Map<T>(CommandArguments commandArguments) where T : new()
    {
        var payload = new T();
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            var argAttribute = property.GetCustomAttribute<ArgAttribute>();
            if (argAttribute != null)
            {
                var value = commandArguments.GetValue(argAttribute.Name, argAttribute.DefaultValue);
                property.SetValue(payload, value);
            }
        }
        return payload;
    }
}
