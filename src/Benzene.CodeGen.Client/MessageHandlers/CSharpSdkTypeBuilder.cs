using System.Reflection;
using Benzene.Abstractions.MessageHandling;
using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;

namespace Benzene.CodeGen.Client.MessageHandlers
{
    public class CSharpSdkTypeBuilder : ICodeBuilder<IMessageHandlerDefinition[]>
    {
        private readonly IDictionary<string, string> _propertyTypeMapping = new Dictionary<string, string>
        {
            { "String", "string" },
            { "String[]", "string[]" },
            { "Object", "object" },
        };

        private readonly string _serviceName;
        private readonly string _baseNamespace;

        public CSharpSdkTypeBuilder(string serviceName, string baseNamespace)
        {
            _baseNamespace = baseNamespace;
            _serviceName = serviceName;
        }

        public ICodeFile[] BuildCodeFiles(IMessageHandlerDefinition[] dictionary)
        {
            var allTypes = GetAllTypes(dictionary);
            return allTypes.Select(BuildType).ToArray();
        }

        private static Type[] GetAllTypes(IMessageHandlerDefinition[] dictionary)
        {
            return dictionary
                .Select(x => x.RequestType)
                .Concat(dictionary.Select(x => x.ResponseType))
                .SelectMany(ExtractInnerType)
                .Distinct()
                .Where(IsNotValueType)
                .ToArray();
        }

        private static Type[] ExtractInnerType(Type type)
        {
            var output = new List<Type>();

            output.Add(GetInnerType(type));

            output.AddRange(GetSubTypes(type));
            return output
                .Where(IsNotValueType)
                .ToArray();
        }

        private static Type GetInnerType(Type type)
        {
            return type.IsArray
                ? type.GetElementType()
                : type;
        }

        private static Type[] GetSubTypes(Type type)
        {
            return type.GetProperties()
                .Select(x => GetInnerType(x.PropertyType))
                .Where(IsNotValueType)
                .SelectMany(ExtractInnerType)
                .ToArray();
        }

        public static bool IsNotValueType(Type type)
        {
            return !type.IsArray &&
                   !type.IsValueType &&
                   type.Name != "String" &&
                   type.Name != "Datetime" &&
                   type.Name != "Object" &&
                   type.Name != "Dictionary`2";
        }

        public ICodeFile BuildType(Type type)
        {
            if (type.GetInterfaces().Any(x => x.Name == "IUpdateMessage"))
            {
                return BuildPatchType(type);
            }

            return BuildSimpleType(type);
        }

        private ICodeFile BuildSimpleType(Type type)
        {
            var lineWriter = new LineWriter();

            foreach (var usingStatement in GetUsingStatements(type))
            {
                lineWriter.WriteLine($"using {usingStatement};");
            }
            lineWriter.WriteLine("");
            lineWriter.WriteLine($"namespace {_baseNamespace}.{_serviceName}");
            lineWriter.WriteLine("{");
            lineWriter.WriteLine($"public class {FormatTypeClassName(type)}", 1);
            lineWriter.WriteLine("{", 1);

            foreach (var property in GetProperties(type))
            {
                lineWriter.WriteLine($"public {property.Value} {property.Key} {{ get; set; }}", 2);
            }

            lineWriter.WriteLine("}", 1);
            lineWriter.WriteLine("}");

            return new CodeFile($"{FormatTypeName(type)}.cs", lineWriter.GetLines());
        }

        private ICodeFile BuildPatchType(Type type)
        {
            var lineWriter = new LineWriter();

            foreach (var usingStatement in GetUsingStatements(type))
            {
                lineWriter.WriteLine($"using {usingStatement};");
            }
            lineWriter.WriteLine("using benzene.Elements.LambdaClients.Core;");
            lineWriter.WriteLine("");
            lineWriter.WriteLine($"namespace {_baseNamespace}.{_serviceName}");
            lineWriter.WriteLine("{");
            lineWriter.WriteLine($"public class {FormatTypeClassName(type)} : UpdateMessage", 1);
            lineWriter.WriteLine("{", 1);

            foreach (var property in GetProperties(type))
            {
                if (property.Key.ToLowerInvariant() == "updatefields")
                {
                    continue;
                }

                var camelCaseName = CodeGenHelpers.Camelcase(property.Key);
                lineWriter.WriteLine($"private {property.Value} _{camelCaseName};", 2);
                lineWriter.WriteLine($"public {property.Value} {property.Key}", 2);
                lineWriter.WriteLine("{", 2);
                lineWriter.WriteLine($"get => _{camelCaseName};", 3);
                lineWriter.WriteLine($"set {{ AddUpdateField(\"{property.Key.ToLowerInvariant()}\"); _{camelCaseName} = value; }}", 3);
                lineWriter.WriteLine("}", 2);
            }

            lineWriter.WriteLine("}", 1);
            lineWriter.WriteLine("}");

            return new CodeFile($"{FormatTypeName(type)}.cs", lineWriter.GetLines());
        }

        private string[] GetUsingStatements(Type type)
        {
            var output = new List<string>();
            output.Add("System");

            if (type.GetProperties().Any(x => x.PropertyType.Name == "Dictionary`2"))
            {
                output.Add("System.Collections.Generic");
            }

            return output.ToArray();
        }


        public IDictionary<string, string> GetProperties(Type type)
        {
            var replaceGenericTypes = type.GetGenericArguments();

            return type
                .GetProperties()
                .ToDictionary(
                    x => x.Name,
                    x =>
                        replaceGenericTypes.Contains(x.PropertyType)
                            ? "T"
                            : GetPropertyTypeName(x));
        }

        private string FormatTypeName(Type type)
        {
            return type.Name.Split('`').First();
        }

        private string FormatTypeClassName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            return $"{FormatTypeName(type)}<T>";
        }

        private string GetPropertyTypeName(PropertyInfo propertyInfo)
        {
            return GetTypeName(propertyInfo.PropertyType);
        }

        public string GetTypeName(Type type, bool useHasValue)
        {
            if (useHasValue && type.GetInterfaces().Any(x => x.Name.ToLowerInvariant().Contains("ihasid")))
            {
                return GetTypeName(type.GetProperty("Id").PropertyType);
            }

            return GetTypeName(type);
        }

        public string GetTypeName(Type type)
        {
            if (type.Name == "Nullable`1")
            {
                return $"{type.GenericTypeArguments[0].Name}?";
            }

            if (type.Name == "Dictionary`2")
            {
                return $"Dictionary<{GetTypeName(type.GenericTypeArguments[0])}, {GetTypeName(type.GenericTypeArguments[1])}>";
            }

            if (type.IsGenericType)
            {
                return $"{type.Name.Split('`').First()}<{GetTypeName(type.GenericTypeArguments[0])}>";
            }

            return _propertyTypeMapping.ContainsKey(type.Name)
                ? _propertyTypeMapping[type.Name]
                : type.Name;
        }

        private static string GetMethodName(Type type)
        {
            return type.Name.Replace("Message", "");
        }

    }
}
