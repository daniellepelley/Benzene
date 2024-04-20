namespace Benzene.Abstractions;

public interface IHttpBuilder
{
    IDictionary<string, string> Headers { get; }
    string Method { get; }
    string Path { get; }
    object? Message { get; }
}
