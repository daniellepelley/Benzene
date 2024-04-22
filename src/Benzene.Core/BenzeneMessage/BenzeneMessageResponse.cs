using System.Collections.Generic;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageResponse : IBenzeneMessageResponse
{
    public string StatusCode { get; set; }
    public IDictionary<string, string> Headers { get; set; }
    public string Message { get; set; }
}