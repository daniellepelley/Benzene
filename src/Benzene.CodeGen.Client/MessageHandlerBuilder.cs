using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Client;

public class MessageHandlerBuilder : ICodeBuilder<EventServiceDocument>
{
    private readonly string _baseNamespace;
    private readonly ICodeBuilder<IDictionary<string, OpenApiSchema>> _typeBuilder;
    private readonly IMethodName _methodName;
    private readonly ITypeName _typeName;

    public MessageHandlerBuilder(string baseNamespace)
        :this(baseNamespace, new OpenApiSchemaCSharpTypeBuilder(baseNamespace), new CSharpTypeName(), new TopicMethodName())
    { }
    
    public MessageHandlerBuilder(string baseNamespace, ICodeBuilder<IDictionary<string, OpenApiSchema>> typeBuilder, ITypeName typeName, IMethodName methodName)
    {
        _baseNamespace = baseNamespace;
        _typeBuilder = typeBuilder;
        _typeName = typeName;
        _methodName = methodName;
    }

    public ICodeFile[] BuildCodeFiles(EventServiceDocument eventServiceDocument)
    {
        var output = new List<ICodeFile>();

        foreach (var definition in eventServiceDocument.Requests)
        {
            output.Add(AddHandler(definition.Topic, definition.Request, definition.Response));
        }

        foreach (var codeFile in _typeBuilder.BuildCodeFiles(eventServiceDocument.Components.Schemas))
        {
            output.Add(codeFile);
        }

        return output.ToArray();
    }


    private ICodeFile AddHandler(string topic, OpenApiSchema requestType, OpenApiSchema responseType)
    {
        var name= _methodName.Create(topic, new OpenApiSchema());
        var requestTypeName = _typeName.GetName(requestType);
        var responseTypeName = _typeName.GetName(responseType);

        var lineWriter = new LineWriter();

        lineWriter.WriteLine("using Benzene.Abstractions.MessageHandling;");
        lineWriter.WriteLine("using Benzene.Results;");
        lineWriter.WriteLine("");

        lineWriter.WriteLine($"namespace {_baseNamespace};");
        lineWriter.WriteLine("");
        lineWriter.WriteLine($"[Message(\"{topic}\")]");
        lineWriter.WriteLine($"public class {name}MessageHandler : IMessageHandler<{requestTypeName}, {responseTypeName}>");
        lineWriter.WriteLine("{");
        lineWriter.WriteLine($"public {name}MessageHandler()", 1);
        lineWriter.WriteLine("{", 1);
        lineWriter.WriteLine("//inject any dependencies here", 2);
        lineWriter.WriteLine("}", 1);
        lineWriter.WriteLine("");
        lineWriter.WriteLine($"public async Task<IBenzeneResult<{responseTypeName}>> HandleAsync({requestTypeName} message)", 1);
        lineWriter.WriteLine("{", 1);
        lineWriter.WriteLine($"return await Task.FromResult(ServiceResult.NotImplemented<{responseTypeName}>());", 2);
        lineWriter.WriteLine("}", 1);
        lineWriter.WriteLine("}");

        return new CodeFile($"{name}MessageHandler.cs", lineWriter.GetLines());

    }
}
