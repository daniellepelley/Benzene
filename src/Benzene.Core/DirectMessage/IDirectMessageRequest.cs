using System.Collections.Generic;

namespace Benzene.Core.DirectMessage;

public interface IDirectMessageRequest
{
    string Topic { get; }
    IDictionary<string, string> Headers { get; }
    string Message { get; }
}