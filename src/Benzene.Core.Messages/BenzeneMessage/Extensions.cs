namespace Benzene.Core.Messages.BenzeneMessage;

public static class Extensions
{
    public static void EnsureResponseExists(this BenzeneMessageContext context)
    {
        context.BenzeneMessageResponse ??= new BenzeneMessageResponse();
        context.BenzeneMessageResponse.Headers ??= new Dictionary<string, string>();
    }
}