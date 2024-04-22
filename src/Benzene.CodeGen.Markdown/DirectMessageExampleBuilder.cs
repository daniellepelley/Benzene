using Benzene.CodeGen.Core;
using Newtonsoft.Json;

namespace Benzene.CodeGen.Markdown;

public class BenzeneMessageExampleBuilder : ExampleBuilder 
{

    public BenzeneMessageExampleBuilder(IDictionary<string, object> knownValues)
        :base("Direct", (topic, payload) => new
        {
            topic,
            message = JsonConvert.SerializeObject(payload)
        }, knownValues)
    {
    }
}
