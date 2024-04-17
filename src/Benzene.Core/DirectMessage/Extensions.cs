using System.Collections.Generic;

namespace Benzene.Core.DirectMessage;

public static class Extensions
{
    public static void EnsureResponseExists(this DirectMessageContext context)
    {
        context.DirectMessageResponse ??= new DirectMessageResponse();
        context.DirectMessageResponse.Headers ??= new Dictionary<string, string>();
    }
}