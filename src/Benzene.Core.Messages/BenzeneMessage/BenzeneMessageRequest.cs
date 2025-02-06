namespace Benzene.Core.Messages.BenzeneMessage;

public class BenzeneMessageRequest : IBenzeneMessageRequest
{
    public string Topic { get; set; }
    public IDictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
}