using Benzene.CodeGen.Core.Writers;
using Benzene.Schema.OpenApi.EventService;
using Benzene.Schema.OpenApi.Examples;
using Newtonsoft.Json;

namespace Benzene.CodeGen.Core;

public class ExampleBuilder : IExampleBuilder
{
    private readonly Func<string, object, object> _messageBuilder;
    private readonly IDictionary<string, object> _knownValues;

    public ExampleBuilder(string transport, Func<string, object, object> messageBuilder, IDictionary<string, object> knownValues)
    {
        _knownValues = knownValues;
        _messageBuilder = messageBuilder;
        Transport = transport;
    }

    public void BuildExample(RequestResponse requestResponse, ISchemaGetter schemaGetter,
        ILineWriter lineWriter)
    {
        var examplePayloadBuilder = new ExamplePayloadBuilder(_knownValues);

        var message = _messageBuilder(requestResponse.Topic, examplePayloadBuilder.Build(schemaGetter.GetOpenApiSchema(requestResponse.Request), schemaGetter));

        var json = JsonConvert.SerializeObject(message,
            new JsonSerializerSettings { Formatting = Formatting.Indented });
        lineWriter.WriteLines(json.Split(Environment.NewLine));
    }

    public string Transport { get; }
}
