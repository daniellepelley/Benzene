namespace Benzene.Core.Versioning.Deserializer;

public interface IPayloadSchemaVersionLookUp
{
    string GetSchemaVersion(string topic);
}
