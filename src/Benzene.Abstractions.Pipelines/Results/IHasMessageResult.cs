namespace Benzene.Abstractions.Results;

public interface IHasMessageResult
{
    IMessageResult MessageResult { get; set; }
}