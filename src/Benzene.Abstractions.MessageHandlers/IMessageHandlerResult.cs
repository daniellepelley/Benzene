using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerResultBase
{
    ITopic? Topic { get; }
    IMessageHandlerDefinition? MessageHandlerDefinition { get; }
}

public interface IMessageHandlerResult : IMessageHandlerResultBase
{
    IBenzeneResult BenzeneResult { get; }
}

public interface IMessageHandlerResult<TResponse> : IMessageHandlerResultBase
{
    IBenzeneResult<TResponse> BenzeneResult { get; }
}