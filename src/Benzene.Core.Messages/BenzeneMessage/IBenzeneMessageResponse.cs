using System.Collections.Generic;

namespace Benzene.Core.BenzeneMessage;

public interface IBenzeneMessageResponse
{
    string StatusCode { get; set; }
    IDictionary<string, string> Headers { get; set; }
    string Body { get; set; }
}