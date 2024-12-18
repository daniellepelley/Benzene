namespace Benzene.Abstractions;

public interface IHttpBuilder<T>
{
    IDictionary<string, string> Headers { get; }
    string Method { get; }
    string Path { get; }
    T? Message { get; }
}
