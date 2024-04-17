namespace Benzene.Results;

public interface IServiceResult : IResult
{}

public interface IServiceResult<T> : IResult<T>, IServiceResult
{}