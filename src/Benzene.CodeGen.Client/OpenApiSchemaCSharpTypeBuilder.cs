using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Client;

public class OpenApiSchemaCSharpTypeBuilder : ICodeBuilder<IDictionary<string, OpenApiSchema>>
{
    private readonly string _baseNamespace;
    private readonly INameFormatter _nameFormatter;
    private readonly ITypeName _typeName;

    public OpenApiSchemaCSharpTypeBuilder(string baseNamespace)
    {
        _baseNamespace = baseNamespace;
        _nameFormatter = new CSharpNameFormatter();
        _typeName = new CSharpTypeName();
    }

    public ICodeFile[] BuildCodeFiles(IDictionary<string, OpenApiSchema> dictionary)
    {
        return dictionary.Select(BuildType).ToArray();
    }

    public ICodeFile BuildType(KeyValuePair<string, OpenApiSchema> type)
    {
        return BuildSimpleType(type.Key, type.Value);
    }

    private ICodeFile BuildSimpleType(string name, OpenApiSchema schema)
    {
        var lineWriter = new LineWriter();

        foreach (var usingStatement in GetUsingStatements(schema))
        {
            lineWriter.WriteLine($"using {usingStatement};");
        }
        lineWriter.WriteLine("");
        lineWriter.WriteLine($"namespace {_baseNamespace}");
        lineWriter.WriteLine("{");
        lineWriter.WriteLine("[ExcludeFromCodeCoverage]", 1);
        lineWriter.WriteLine($"public class {_nameFormatter.Format(name)}", 1);
        lineWriter.WriteLine("{", 1);

        foreach (var property in schema.Properties)
        {
            lineWriter.WriteLine($"public {GetTypeName(property.Value)} {_nameFormatter.Format(property.Key)} {{ get; set; }}", 2);
        }

        lineWriter.WriteLine("}", 1);
        lineWriter.WriteLine("}");

        return new CodeFile($"{name}.cs", lineWriter.GetLines());
    }

    private (string, string[]) BuildPatchType(string name, OpenApiSchema type)
    {
        var lineWriter = new LineWriter();

        foreach (var usingStatement in GetUsingStatements(type))
        {
            lineWriter.WriteLine($"using {usingStatement};");
        }
        lineWriter.WriteLine("using benzene.Elements.LambdaClients.Core;");
        lineWriter.WriteLine("");
        lineWriter.WriteLine($"namespace {_baseNamespace}");
        lineWriter.WriteLine("{");
        lineWriter.WriteLine($"public class {name} : UpdateMessage", 1);
        lineWriter.WriteLine("{", 1);

        foreach (var property in type.Properties)
        {
            if (property.Key.ToLowerInvariant() == "updatefields")
            {
                continue;
            }

            var camelCaseName = CodeGenHelpers.Camelcase(new FormatString(property.Key));
            lineWriter.WriteLine($"private {property.Value} _{camelCaseName};", 2);
            lineWriter.WriteLine($"public {property.Value} {property.Key}", 2);
            lineWriter.WriteLine("{", 2);
            lineWriter.WriteLine($"get => _{camelCaseName};", 3);
            lineWriter.WriteLine($"set {{ AddUpdateField(\"{property.Key.ToLowerInvariant()}\"); _{camelCaseName} = value; }}", 3);
            lineWriter.WriteLine("}", 2);
        }

        lineWriter.WriteLine("}", 1);
        lineWriter.WriteLine("}");

        return ($"{name}.cs", lineWriter.GetLines());
    }

    private string[] GetUsingStatements(OpenApiSchema schema)
    {
        var output = new List<string>();
        output.Add("System");
        output.Add("System.Diagnostics.CodeAnalysis");

        if (schema.Properties.Any(x => x.Value.AdditionalProperties != null && x.Value.AdditionalProperties.Type == "string"))
        {
            output.Add("System.Collections.Generic");
        }

        return output.ToArray();
    }

    public string GetTypeName(OpenApiSchema openApiSchema)
    {
        return _typeName.GetName(openApiSchema);
    }
}
