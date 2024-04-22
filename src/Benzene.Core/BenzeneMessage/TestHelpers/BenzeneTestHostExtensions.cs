using System.Threading.Tasks;
using Benzene.Abstractions;
using Benzene.Core.BenzeneMessage.TestHelpers;

namespace Benzene.Core.BenzeneMessage;

public static class BenzeneTestHostExtensions
{
    public static Task<BenzeneMessageResponse> SendBenzeneMessageAsync(this IBenzeneTestHost source, BenzeneMessageRequest benzeneMessageRequest)
    {
        return source.SendEventAsync<BenzeneMessageResponse>(benzeneMessageRequest);
    }

    public static Task<BenzeneMessageResponse> SendBenzeneMessageAsync(this IBenzeneTestHost source, IMessageBuilder messageBuilder)
    {
        return source.SendBenzeneMessageAsync(messageBuilder.AsBenzeneMessage());
    }
}
