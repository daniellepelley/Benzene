using System.Collections.Generic;

namespace Benzene.Core.BenzeneMessage;

public interface IBenzeneMessageRequest
{
    string Topic { get; }
    IDictionary<string, string> Headers { get; }
    string Body { get; }
}