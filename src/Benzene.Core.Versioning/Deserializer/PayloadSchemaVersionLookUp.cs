namespace Benzene.Core.Versioning.Deserializer;

public class PayloadSchemaVersionLookUp(Dictionary<string, string> dictionary) : IPayloadSchemaVersionLookUp
{
    public string GetSchemaVersion(string topic) => dictionary[topic];
}

