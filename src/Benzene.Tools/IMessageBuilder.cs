namespace Benzene.Tools;

public interface IMessageBuilder
{
    IDictionary<string, string> Headers { get; }
    string Topic { get; }
    object Message { get; }
}