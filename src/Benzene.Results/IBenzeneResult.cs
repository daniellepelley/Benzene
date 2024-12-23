namespace Benzene.Results;

public interface IBenzeneResult
{
    string Status { get; }
    bool IsSuccessful { get; }
    object PayloadAsObject { get; }
    string[] Errors { get; }
}

public interface IBenzeneResult<T> : IBenzeneResult
{
    T Payload { get; }
}