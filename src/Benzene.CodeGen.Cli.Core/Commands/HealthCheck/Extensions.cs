using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Benzene.CodeGen.Cli.Core.Commands.HealthCheck;

public static class Extensions
{
    public static void WriteJson(this TextWriter source, string json)
    {
        var output = JValue.Parse(json).ToString(Formatting.Indented);
        source.WriteLine(output);
    }
}