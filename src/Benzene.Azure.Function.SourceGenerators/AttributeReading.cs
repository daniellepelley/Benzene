using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Benzene.Azure.Function.SourceGenerators
{
    /// <summary>Shared helpers for turning attribute arguments into safe, fully-qualified emitted C#.</summary>
    internal static class AttributeReading
    {
        /// <summary>A correctly-escaped, quoted C# string literal (user-authored values may contain quotes/backslashes).</summary>
        public static string Literal(string? value) => SymbolDisplay.FormatLiteral(value ?? string.Empty, quote: true);

        /// <summary>Reads a named string argument, or a default when absent.</summary>
        public static string NamedString(AttributeData attribute, string name, string fallback)
        {
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Key == name && arg.Value.Value is string s)
                {
                    return s;
                }
            }

            return fallback;
        }

        /// <summary>Reads a named bool argument, or a default when absent.</summary>
        public static bool NamedBool(AttributeData attribute, string name, bool fallback)
        {
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Key == name && arg.Value.Value is bool b)
                {
                    return b;
                }
            }

            return fallback;
        }

        /// <summary>Reads a named string[] argument as a comma-separated list of quoted literals, or a fallback list.</summary>
        public static string NamedStringArrayCsv(AttributeData attribute, string name, params string[] fallback)
        {
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Key == name && arg.Value.Kind == TypedConstantKind.Array && !arg.Value.IsNull)
                {
                    var items = arg.Value.Values
                        .Select(v => Literal(v.Value as string))
                        .Where(v => v != "\"\"");
                    var csv = string.Join(", ", items);
                    if (csv.Length > 0)
                    {
                        return csv;
                    }
                }
            }

            return string.Join(", ", fallback.Select(Literal));
        }

        /// <summary>
        /// Reads a named enum argument as a fully-qualified member expression (e.g.
        /// <c>global::Microsoft.Azure.Functions.Worker.AuthorizationLevel.Anonymous</c>), or the given
        /// default member expression when absent.
        /// </summary>
        public static string NamedEnumMember(AttributeData attribute, string name, string fallbackExpression)
        {
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Key == name && arg.Value.Kind == TypedConstantKind.Enum && arg.Value.Type is INamedTypeSymbol enumType)
                {
                    var member = enumType.GetMembers()
                        .OfType<IFieldSymbol>()
                        .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, arg.Value.Value));

                    if (member != null)
                    {
                        return "global::" + enumType.ToDisplayString() + "." + member.Name;
                    }

                    // Unnamed/combined value: emit a cast so it still compiles.
                    return "(global::" + enumType.ToDisplayString() + ")" + arg.Value.Value;
                }
            }

            return fallbackExpression;
        }

        /// <summary>The single required trigger name (first constructor argument), or null if absent/empty.</summary>
        public static string? TriggerName(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string s && s.Length > 0)
            {
                return s;
            }

            return null;
        }

        /// <summary>Turns an arbitrary trigger name into a valid, stable C# identifier for the generated class.</summary>
        public static string ToIdentifier(string name)
        {
            var sb = new StringBuilder();
            var capitalizeNext = true;
            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(capitalizeNext ? char.ToUpperInvariant(ch) : ch);
                    capitalizeNext = false;
                }
                else
                {
                    // Any separator (-, :, /, space, …) becomes a word boundary.
                    capitalizeNext = true;
                }
            }

            if (sb.Length == 0 || char.IsDigit(sb[0]))
            {
                sb.Insert(0, '_');
            }

            return sb.ToString();
        }
    }
}
