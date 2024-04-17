using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Markdown;

public class LambdaServiceMarkdownBuilder : ICodeBuilder<EventServiceDocument>
{
    private readonly string _lambdaName;
    private readonly string _serviceName;
    private readonly string _headerText;
    private readonly IExampleBuilder[] _exampleBuilders;

    public LambdaServiceMarkdownBuilder(string lambdaName, string serviceName, string headerText)
        : this(lambdaName, serviceName, headerText, new IExampleBuilder[] { new DirectMessageExampleBuilder(new Dictionary<string, object>()) })
    { }

    public LambdaServiceMarkdownBuilder(string lambdaName, string serviceName, string headerText, IExampleBuilder[] exampleBuilders)
    {
        _headerText = headerText;
        _serviceName = serviceName;
        _lambdaName = lambdaName;
        _exampleBuilders = exampleBuilders;
    }
    
    public ICodeFile[] BuildCodeFiles(EventServiceDocument eventServiceDocument)
    {
        return new ICodeFile[] {
            new CodeFile("README.md", BuildDocument(eventServiceDocument))
        };
    }

    private string[] BuildDocument(EventServiceDocument eventServiceDocument)
    {
        var schemaGetter = new SchemaGetter(eventServiceDocument.Components.Schemas);
        var markdownTypeBuilder = new MarkdownTypeBuilder(schemaGetter);
        var lineWriter = new LineWriter();

        lineWriter.WriteLine($"# {_lambdaName}");
        lineWriter.WriteLine($"## {_serviceName} Service");
        lineWriter.WriteLine(_headerText);
        lineWriter.WriteLine("## Messages");

        foreach (var requestResponse in eventServiceDocument.Requests)
        {
            lineWriter.WriteLine($"> # {requestResponse.Topic}");
            lineWriter.WriteLine("## *Request*");
            lineWriter.WriteLine("```");
            markdownTypeBuilder.BuildType(requestResponse.Request, lineWriter);
            lineWriter.WriteLine("```");
            lineWriter.WriteLines(BuildValidation(schemaGetter.GetOpenApiSchema(requestResponse.Request)));
            lineWriter.WriteLine();
            BuildExamples(requestResponse, schemaGetter, lineWriter);
            lineWriter.WriteLine();
            lineWriter.WriteLine("## *Responses*");
            lineWriter.WriteLine($"> ## {requestResponse.Topic}:result");
            lineWriter.WriteLine("*Response to the sender*");
            lineWriter.WriteLine("```");
            markdownTypeBuilder.BuildType(requestResponse.Response, lineWriter);
            lineWriter.WriteLine("```");

            lineWriter.WriteLine("&nbsp;");
            lineWriter.WriteLine("");
            lineWriter.WriteLine("---");
            lineWriter.WriteLine("&nbsp;");
        }

        return lineWriter.GetLines();
    }

    private string[] BuildValidation(OpenApiSchema openApiSchema)
    {
        if (!openApiSchema.Properties.Any())
        {
            return Array.Empty<string>();
        }

        var lineWriter = new LineWriter();

        lineWriter.WriteLine("### Validation");
        lineWriter.WriteLine("| **Field** | **Validation** |");
        lineWriter.WriteLine("| - | - |");

        foreach (var property in openApiSchema.Properties)
        {
            var validationRules = string.Join(", ", ValidationRules(property.Value));
            lineWriter.WriteLine($"|{CodeGenHelpers.Camelcase(property.Key)}|{validationRules}|");
        }

        return lineWriter.GetLines();
    }

    private string[] ValidationRules(OpenApiSchema openApiSchema)
    {
        var output = new List<string>();

        if (!openApiSchema.Nullable)
        {
            output.Add("Not Null");
        }

        if (openApiSchema.MaxLength.HasValue)
        {
            output.Add($"Maximum Length of {openApiSchema.MaxLength} characters");
        }

        return output.ToArray();
    }

    private void BuildExamples(RequestResponse requestResponse, ISchemaGetter schemaGetter, ILineWriter lineWriter)
    {
        foreach (var exampleBuilder in _exampleBuilders)
        {
            lineWriter.WriteLine($"### Example - {exampleBuilder.Transport}");
            lineWriter.WriteLine("```");
            exampleBuilder.BuildExample(requestResponse, schemaGetter, lineWriter);
            lineWriter.WriteLine("```");
        }
    }
}
