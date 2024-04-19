using Benzene.CodeGen.Core;
using Newtonsoft.Json;

namespace Benzene.CodeGen.Markdown;

public class DirectMessageExampleBuilder : ExampleBuilder 
{

    public DirectMessageExampleBuilder(IDictionary<string, object> knownValues)
        :base("Direct", (topic, payload) => new
        {
            topic,
            message = JsonConvert.SerializeObject(payload)
        }, knownValues)
    {
    }
}
