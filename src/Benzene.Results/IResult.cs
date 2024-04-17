namespace Benzene.Results;

public interface IResult
{
    string Status { get; }
    bool IsSuccessful { get; }
    object PayloadAsObject { get; }
    string[] Errors { get; }
}

public interface IResult<T> : IResult
{
    T Payload { get; }
}