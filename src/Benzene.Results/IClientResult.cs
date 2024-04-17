namespace Benzene.Results
{
    public interface IClientResult : IResult
    {
    }

    public interface IClientResult<T> : IResult<T>, IClientResult
    {
    }
}