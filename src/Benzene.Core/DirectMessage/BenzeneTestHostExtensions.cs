using System.Threading.Tasks;
using Benzene.Abstractions;

namespace Benzene.Core.DirectMessage;

public static class BenzeneTestHostExtensions
{
    public static Task<DirectMessageResponse> SendDirectMessageAsync(this IBenzeneTestHost source, DirectMessageRequest directMessageRequest)
    {
        return source.SendEventAsync<DirectMessageResponse>(directMessageRequest);
    }

    public static Task<DirectMessageResponse> SendDirectMessageAsync(this IBenzeneTestHost source, IMessageBuilder messageBuilder)
    {
        return source.SendDirectMessageAsync(messageBuilder.AsDirectMessage());
    }
}
