namespace Benzene.Core.Messages.BenzeneMessage;

public class BenzeneMessageResponse : IBenzeneMessageResponse
{
    public string StatusCode { get; set; }
    public IDictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
}