namespace Benzene.Abstractions.Messages;

public interface IMessageDefinitionFinder<TMessageDefinition> where TMessageDefinition : IMessageDefinition
{
    TMessageDefinition[] FindDefinitions();
}