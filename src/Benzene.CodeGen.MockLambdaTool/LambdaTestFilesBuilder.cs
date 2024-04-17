using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Benzene.Schema.OpenApi.EventService;

namespace Benzene.CodeGen.LambdaTestTool;

public class LambdaTestFilesBuilder : ICodeBuilder<EventServiceDocument>
{
    private readonly IExampleBuilder[] _exampleBuilders;

    public LambdaTestFilesBuilder(IExampleBuilder[] exampleBuilders)
    {
        _exampleBuilders = exampleBuilders;
    }

    public ICodeFile[] BuildCodeFiles(EventServiceDocument eventServiceDocument)
    {
        var schemaGetter = new SchemaGetter(eventServiceDocument.Components.Schemas);
        var codeFiles = new List<ICodeFile>();
        foreach (var requestResponse in eventServiceDocument.Requests)
        {
            var filePrefix = requestResponse.Topic.Replace(":", "-");

            foreach (var exampleBuilder in _exampleBuilders)
            {
                var lineWriter = new LineWriter();
                exampleBuilder.BuildExample(requestResponse, schemaGetter, lineWriter);
                var lines = lineWriter.GetLines();
                if (lines.Any())
                {
                    codeFiles.Add(new CodeFile($"{filePrefix}-{FormatTransport(exampleBuilder.Transport)}.json", lineWriter.GetLines()));
                }
            }
        }
        return codeFiles.ToArray();
    }

    private static string FormatTransport(string transport)
    {
        return transport.Replace(" ", "-").ToLowerInvariant();
    }
}
