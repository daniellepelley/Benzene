namespace Benzene.Core.Messages.BenzeneMessage;

public interface IBenzeneMessageRequest
{
    string Topic { get; }
    IDictionary<string, string> Headers { get; }
    string Body { get; }
}