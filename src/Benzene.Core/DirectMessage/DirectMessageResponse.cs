using System.Collections.Generic;

namespace Benzene.Core.DirectMessage;

public class DirectMessageResponse : IDirectMessageResponse
{
    public string StatusCode { get; set; }
    public IDictionary<string, string> Headers { get; set; }
    public string Message { get; set; }
}