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
        return dictionary.Select(type => BuildSimpleType(type.Key, type.Value, dictionary)).ToArray();
    }

    public ICodeFile BuildType(KeyValuePair<string, OpenApiSchema> type)
    {
        return BuildSimpleType(type.Key, type.Value, null);
    }

    private ICodeFile BuildSimpleType(string name, OpenApiSchema schema,
        IDictionary<string, OpenApiSchema>? catalogue)
    {
        // allOf composition: a single $ref branch is the base type; inline branches carry the
        // schema's own properties (Swashbuckle also leaves own properties at the top level).
        var baseTypeId = schema.AllOf?.FirstOrDefault(x => x.Reference != null)?.Reference.Id;
        var ownProperties = GetOwnProperties(schema);
        var hasDiscriminator = schema.Discriminator?.PropertyName is { Length: > 0 } &&
                               schema.Discriminator.Mapping is { Count: > 0 };

        var lineWriter = new LineWriter();

        foreach (var usingStatement in GetUsingStatements(schema, hasDiscriminator))
        {
            lineWriter.WriteLine($"using {usingStatement};");
        }
        lineWriter.WriteLine("");
        lineWriter.WriteLine($"namespace {_baseNamespace}");
        lineWriter.WriteLine("{");
        lineWriter.WriteLine("[ExcludeFromCodeCoverage]", 1);

        if (hasDiscriminator)
        {
            // Mirror the contract's discriminator as System.Text.Json polymorphism attributes so
            // the generated hierarchy round-trips derived instances the way the spec describes.
            lineWriter.WriteLine(
                $"[JsonPolymorphic(TypeDiscriminatorPropertyName = \"{schema.Discriminator!.PropertyName}\")]", 1);
            foreach (var mapping in schema.Discriminator.Mapping)
            {
                lineWriter.WriteLine(
                    $"[JsonDerivedType(typeof({_nameFormatter.Format(RefName(mapping.Value))}), \"{mapping.Key}\")]", 1);
            }
        }

        var declaration = $"public class {_nameFormatter.Format(name)}";
        if (!string.IsNullOrEmpty(baseTypeId))
        {
            declaration += $" : {_nameFormatter.Format(baseTypeId)}";
        }
        lineWriter.WriteLine(declaration, 1);
        lineWriter.WriteLine("{", 1);

        foreach (var property in ownProperties)
        {
            // The discriminator is serializer metadata ([JsonPolymorphic] writes it); a real
            // property of the same name would clash with it on serialization.
            if (hasDiscriminator && property.Key == schema.Discriminator!.PropertyName)
            {
                continue;
            }

            lineWriter.WriteLine(
                $"public {GetTypeName(property.Value, catalogue)} {_nameFormatter.Format(property.Key)} {{ get; set; }}", 2);
        }

        lineWriter.WriteLine("}", 1);
        lineWriter.WriteLine("}");

        return new CodeFile($"{name}.cs", lineWriter.GetLines());
    }

    private static IEnumerable<KeyValuePair<string, OpenApiSchema>> GetOwnProperties(OpenApiSchema schema)
    {
        var inlineAllOfProperties = schema.AllOf?
            .Where(x => x.Reference == null && x.Properties != null)
            .SelectMany(x => x.Properties) ?? Enumerable.Empty<KeyValuePair<string, OpenApiSchema>>();

        return schema.Properties
            .Concat(inlineAllOfProperties)
            .GroupBy(x => x.Key)
            .Select(x => x.First());
    }

    private string[] GetUsingStatements(OpenApiSchema schema, bool hasDiscriminator)
    {
        var output = new List<string>();
        output.Add("System");
        output.Add("System.Diagnostics.CodeAnalysis");

        if (schema.Properties.Any(x => x.Value.AdditionalProperties != null && x.Value.AdditionalProperties.Type == "string"))
        {
            output.Add("System.Collections.Generic");
        }

        if (hasDiscriminator)
        {
            output.Add("System.Text.Json.Serialization");
        }

        return output.ToArray();
    }

    public string GetTypeName(OpenApiSchema openApiSchema)
    {
        return GetTypeName(openApiSchema, null);
    }

    private string GetTypeName(OpenApiSchema openApiSchema, IDictionary<string, OpenApiSchema>? catalogue)
    {
        // A oneOf union member site: type it as the subtypes' shared base class when one is
        // discoverable from the catalogue (their common allOf base $ref), else fall back to object.
        if (openApiSchema?.OneOf is { Count: > 0 } oneOf && oneOf.All(x => x.Reference != null))
        {
            var baseTypeIds = oneOf
                .Select(x => catalogue != null && catalogue.TryGetValue(x.Reference.Id, out var subtype)
                    ? subtype.AllOf?.FirstOrDefault(branch => branch.Reference != null)?.Reference.Id
                    : null)
                .Distinct()
                .ToArray();

            return baseTypeIds is [{ Length: > 0 } sharedBase]
                ? _nameFormatter.Format(sharedBase)
                : "object";
        }

        return _typeName.GetName(openApiSchema);
    }

    private static string RefName(string reference) =>
        reference.Substring(reference.LastIndexOf('/') + 1);
}
