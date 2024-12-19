namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageFinder<TMessageDefinition> where TMessageDefinition : IMessageDefinition
{
    TMessageDefinition[] FindDefinitions();
}