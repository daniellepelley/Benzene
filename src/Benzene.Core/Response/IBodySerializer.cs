using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Response;

public interface IBodySerializer
{
    string Serialize(ISerializer serializer);
}