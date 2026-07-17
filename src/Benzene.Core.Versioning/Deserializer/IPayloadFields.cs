using System.Text.Json;

namespace Benzene.Core.Versioning.Deserializer;

public interface IPayloadFields
{
    string GetSchemaVersion(JsonElement element);
    string GetTopic(JsonElement element);
}
