using Benzene.CodeGen.Core.Writers;
using Benzene.Schema.OpenApi.EventService;

namespace Benzene.CodeGen.Core;

public interface IExampleBuilder
{
    string Transport { get; }
    void BuildExample(RequestResponse requestResponse, ISchemaGetter schemaGetter, ILineWriter lineWriter);
}
