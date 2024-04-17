namespace Benzene.Abstractions.Serialization;

public interface ISerializer
{
    string Serialize(Type type, object payload);
    string Serialize<T>(T payload);
    object? Deserialize(Type type, string payload);
    T? Deserialize<T>(string payload);
}
