namespace Benzene.Core.Correlation;

public interface ICorrelationId
{
    void Set(string correlationId);
    string Get();
}