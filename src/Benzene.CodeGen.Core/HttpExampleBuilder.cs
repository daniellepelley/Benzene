using Benzene.CodeGen.Core.Writers;
using Benzene.Schema.OpenApi.EventService;
using Benzene.Schema.OpenApi.Examples;
using Newtonsoft.Json;

namespace Benzene.CodeGen.Core;

public class HttpExampleBuilder : IExampleBuilder
{
    private readonly IDictionary<string, object> _knownValues;
    private readonly Func<string, string, object, object> _httpMessageBuilder;

    public HttpExampleBuilder(string transport, Func<string, string, object, object> httpMessageBuilder, IDictionary<string, object> knownValues)
    {
        _httpMessageBuilder = httpMessageBuilder;
        _knownValues = knownValues;
        Transport = transport;
    }

    public void BuildExample(RequestResponse requestResponse, ISchemaGetter schemaGetter,
        ILineWriter lineWriter)
    {
        var examplePayloadBuilder = new ExamplePayloadBuilder(_knownValues);

        if (requestResponse.HttpMappings == null)
        {
            return;
        }

        foreach (var httpMapping in requestResponse.HttpMappings)
        {
            var message = _httpMessageBuilder(httpMapping.Method, httpMapping.Path, examplePayloadBuilder.Build(schemaGetter.GetOpenApiSchema(requestResponse.Request), schemaGetter));

            var json = JsonConvert.SerializeObject(message,
                new JsonSerializerSettings { Formatting = Formatting.Indented });
            lineWriter.WriteLines(json.Split(Environment.NewLine));
            lineWriter.WriteLine();
        }
    }

    public string Transport { get; }
}
