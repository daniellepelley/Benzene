namespace Benzene.Abstractions;

public interface IMessageBuilder<T>
{
    IDictionary<string, string> Headers { get; }
    string Topic { get; }
    T? Message { get; }
}