namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageDefinitionFinder<TMessageDefinition> where TMessageDefinition : IMessageDefinition
{
    TMessageDefinition[] FindDefinitions();
}