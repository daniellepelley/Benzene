using System.Reflection;
using System.Text;

namespace Benzene.CodeGen.Cli.Core.Parsing;

public static class HelpGenerator
{
    public static string Generate<T>(string name, string description) where T : new()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"{  name}");
        stringBuilder.AppendLine($"{    description}");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("  Parameters");
        
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            var argAttribute = property.GetCustomAttribute<ArgAttribute>();
            if (argAttribute != null)
            {
                stringBuilder.AppendLine($"    --{argAttribute.Name,-40} {argAttribute.Description}");
            }
        }
        return stringBuilder.ToString();
    }
}
