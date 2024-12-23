using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Client;

public class MessageClientSdkBuilder : ICodeBuilder<EventServiceDocument>
{
    private readonly IDictionary<string, string> _propertyTypeMapping = new Dictionary<string, string>
    {
        { "String", "string" },
        { "String[]", "string[]" },
        { "Object", "object" }
    };

    private readonly string _serviceName;
    private readonly string _baseNamespace;
    private readonly ICodeBuilder<IDictionary<string, OpenApiSchema>> _typeBuilder;
    private readonly IMethodName _methodName;
    private readonly ITypeName _typeName;

    public MessageClientSdkBuilder(string serviceName, string baseNamespace)
        :this(serviceName, baseNamespace, new OpenApiSchemaCSharpTypeBuilder($"{baseNamespace}.{serviceName}"), new CSharpTypeName(), new TopicReversedMethodName())
    { }
    
    public MessageClientSdkBuilder(string serviceName, string baseNamespace, ICodeBuilder<IDictionary<string, OpenApiSchema>> typeBuilder, ITypeName typeName, IMethodName methodName)
    {
        _baseNamespace = baseNamespace;
        _serviceName = serviceName;
        _typeBuilder = typeBuilder;
        _typeName = typeName;
        _methodName = methodName;
    }

    public ICodeFile[] BuildCodeFiles(EventServiceDocument eventServiceDocument)
    {
        var output = new List<ICodeFile>();

        var classString = BuildClass(eventServiceDocument);
        var interfaceString = BuildInterface(eventServiceDocument);

        output.Add(new CodeFile($"{_serviceName}ServiceClient.cs", classString));
        output.Add(new CodeFile($"I{_serviceName}ServiceClient.cs", interfaceString));

        foreach (var codeFile in _typeBuilder.BuildCodeFiles(eventServiceDocument.Components.Schemas))
        {
            output.Add(codeFile);
        }

        return output.ToArray();
    }

    public string[] BuildClass(EventServiceDocument eventServiceDocument)
    {
        var lineWriter = new LineWriter();

        lineWriter.WriteLine("using System;");
        lineWriter.WriteLine("using System.Collections.Generic;");
        lineWriter.WriteLine("using System.Threading.Tasks;");
        lineWriter.WriteLine("using Benzene.Clients;");
        lineWriter.WriteLine("using Benzene.Clients.HealthChecks;");
        lineWriter.WriteLine("using Benzene.HealthChecks.Core;");
        lineWriter.WriteLine("using Benzene.Results;");
        lineWriter.WriteLine("using System.Diagnostics.CodeAnalysis;");
        lineWriter.WriteLine("");

        lineWriter.WriteLine($"namespace {_baseNamespace}.{_serviceName}");
        lineWriter.WriteLine("{");
        lineWriter.WriteLine("[ExcludeFromCodeCoverage]", 1);
        lineWriter.WriteLine($"public class {_serviceName}ServiceClient : I{_serviceName}ServiceClient", 1);
        lineWriter.WriteLine("{", 1);

        lineWriter.WriteLine("private readonly IBenzeneMessageClientFactory _clientFactory;", 2);
        lineWriter.WriteLine();
        lineWriter.WriteLine($"public {_serviceName}ServiceClient(IBenzeneMessageClientFactory clientFactory)", 2);
        lineWriter.WriteLine("{", 2);
        lineWriter.WriteLine("_clientFactory = clientFactory;", 3);
        lineWriter.WriteLine("}", 2);
        lineWriter.WriteLine();

        AddHashCode(eventServiceDocument, lineWriter);

        foreach (var definition in eventServiceDocument.Requests)
        {
            lineWriter.WriteLines(AddMethod(definition.Topic, definition.Request, definition.Response));
        }

        lineWriter.WriteLines(AddHealthCheckMethod());

        lineWriter.WriteLine("}", 1);
        lineWriter.WriteLine("}");

        return lineWriter.GetLines();
    }

    private void AddHashCode(EventServiceDocument eventServiceDocument, LineWriter lineWriter)
    {
        var hashCode = CodeGenHelpers.GenerateHash(eventServiceDocument.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0));
        lineWriter.WriteLine($@"public string HashCode => ""{hashCode}"";", 2);
        lineWriter.WriteLine();
    }

    private string[] AddHealthCheckMethod()
    {
        var lineWriter = new LineWriter();
        lineWriter.WriteLine(
            $"public async Task<IBenzeneResult<HealthCheckResponse>> HealthCheckAsync()", 2);
        lineWriter.WriteLine("{", 2);
        lineWriter.WriteLine($"using (var client = _clientFactory.Create(\"{_serviceName}\", \"healthcheck\"))",
            3);
        lineWriter.WriteLine("{", 3);
        lineWriter.WriteLine(
            $@"var benzeneResult = await client.SendMessageAsync<NullPayload, HealthCheckResponse>(""healthcheck"", new NullPayload(), null);",
            4);
        lineWriter.WriteLine(
            "return benzeneResult.Status != BenzeneResultStatus.Ok",
            4);
        lineWriter.WriteLine("? benzeneResult", 5);
        lineWriter.WriteLine(
            ": BenzeneResult.Ok(ClientHealthCheckProcessor.Process(benzeneResult.Payload, HashCode) as HealthCheckResponse);", 5);
        lineWriter.WriteLine("}", 3);
        lineWriter.WriteLine("}", 2);
        return lineWriter.GetLines();
    }

    private string[] AddMethod(string topic, OpenApiSchema requestType, OpenApiSchema responseType)
    {
        var topicFunction = GetTopicFunction(topic);
        var requestTypeName = _typeName.GetName(requestType);
        var responseTypeName = _typeName.GetName(responseType);
        // var responseTypeName = _typeName.GetName(responseType, new[] { "create", "update" }.Contains(topicFunction));
        var methodName = _methodName.Create(topic, requestType);

        var lineWriter = new LineWriter();
        lineWriter.WriteLine(
            $"public Task<IBenzeneResult<{responseTypeName}>> {methodName}Async({requestTypeName} message)", 2);
        lineWriter.WriteLine("{", 2);
        lineWriter.WriteLine($@"return {methodName}Async(message, null);", 3);
        lineWriter.WriteLine("}", 2);
        lineWriter.WriteLine();

        lineWriter.WriteLine(
            $"public async Task<IBenzeneResult<{responseTypeName}>> {methodName}Async({requestTypeName} message, IDictionary<string, string> headers)", 2);
        lineWriter.WriteLine("{", 2);
        lineWriter.WriteLine($"using (var client = _clientFactory.Create(\"{_serviceName}\", \"{topic}\"))",
            3);
        lineWriter.WriteLine("{", 3);
        lineWriter.WriteLine(
            $@"return await client.SendMessageAsync<{requestTypeName}, {responseTypeName}>(""{topic}"", message, headers);",
            4);
        lineWriter.WriteLine("}", 3);
        lineWriter.WriteLine("}", 2);
        lineWriter.WriteLine();
        return lineWriter.GetLines();
    }

    public string[] BuildInterface(EventServiceDocument eventServiceDocument)
    {
        var lineWriter = new LineWriter();

        lineWriter.WriteLine("using System;");
        lineWriter.WriteLine("using System.Collections.Generic;");
        lineWriter.WriteLine("using System.Threading.Tasks;");
        lineWriter.WriteLine("using Benzene.Clients;");
        lineWriter.WriteLine("using Benzene.Clients.HealthChecks;");
        lineWriter.WriteLine("using Benzene.Results;");
        
        lineWriter.WriteLine("");

        lineWriter.WriteLine($"namespace {_baseNamespace}.{_serviceName}");
        lineWriter.WriteLine("{");
        lineWriter.WriteLine($"public interface I{_serviceName}ServiceClient : IHasHealthCheck", 1);
        lineWriter.WriteLine("{", 1);

        foreach (var definition in eventServiceDocument.Requests)
        {
            var topicFunction = GetTopicFunction(definition.Topic);
            var requestTypeName = _typeName.GetName(definition.Request);
            var responseTypeName = _typeName.GetName(definition.Response);
            var methodName = _methodName.Create(definition.Topic, definition.Request);
    
            lineWriter.WriteLine(
                $"Task<IBenzeneResult<{responseTypeName}>> {methodName}Async({requestTypeName} message);", 2);
            lineWriter.WriteLine(
                $"Task<IBenzeneResult<{responseTypeName}>> {methodName}Async({requestTypeName} message, IDictionary<string, string> headers);", 2);
        }

        lineWriter.WriteLine("}", 1);
        lineWriter.WriteLine("}");

        return lineWriter.GetLines();

    }


    private static string GetTopicFunction(string topic)
    {
        return topic.Split(':').LastOrDefault();
    }

    private string GetTypeName(Type type, bool useHasValue)
    {
        if (useHasValue && type.GetInterfaces().Any(x => x.Name.ToLowerInvariant().Contains("ihasid")))
        {
            return GetTypeName(type.GetProperty("Id").PropertyType);
        }

        return GetTypeName(type);
    }

    private string GetTypeName(Type type)
    {
        if (type.Name == "Nullable`1")
        {
            return $"{type.GenericTypeArguments[0].Name}?";
        }

        return _propertyTypeMapping.ContainsKey(type.Name)
            ? _propertyTypeMapping[type.Name]
            : type.Name;
    }
}
