namespace Benzene.Core.Messages.BenzeneMessage;

public interface IBenzeneMessageResponse
{
    string StatusCode { get; set; }
    IDictionary<string, string> Headers { get; set; }
    string Body { get; set; }
}