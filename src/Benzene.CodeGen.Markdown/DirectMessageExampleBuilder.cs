using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Benzene.Core.DirectMessage;
using Microsoft.OpenApi.Models;
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
