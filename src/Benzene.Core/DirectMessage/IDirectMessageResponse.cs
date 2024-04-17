using System.Collections.Generic;

namespace Benzene.Core.DirectMessage;

public interface IDirectMessageResponse
{
    string StatusCode { get; set; }
    IDictionary<string, string> Headers { get; set; }
    string Message { get; set; }
}