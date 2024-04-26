namespace Benzene.Abstractions;

public interface ICorrelationId
{
    void Set(string correlationId);
    string Get();
}