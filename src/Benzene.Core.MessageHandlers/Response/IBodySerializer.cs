using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Response;

public interface IBodySerializer
{
    string Serialize(ISerializer serializer, IMessageHandlerResult messageHandlerResult);
}