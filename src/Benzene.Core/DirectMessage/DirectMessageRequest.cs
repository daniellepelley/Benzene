using System.Collections.Generic;

namespace Benzene.Core.DirectMessage;

public class DirectMessageRequest : IDirectMessageRequest
{
    public string Topic { get; set; }
    public IDictionary<string, string> Headers { get; set; }
    public string Message { get; set; }
}