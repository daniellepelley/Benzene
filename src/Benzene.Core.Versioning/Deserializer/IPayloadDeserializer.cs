using System.Text.Json;

namespace Benzene.Core.Versioning.Deserializer;

public interface IPayloadDeserializer
{
    T? Deserialize<T>(JsonElement json);
}
