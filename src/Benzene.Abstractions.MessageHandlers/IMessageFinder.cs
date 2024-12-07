namespace Benzene.Abstractions.MessageHandling;

public interface IMessageFinder<TMessageDefinition> where TMessageDefinition : IMessageDefinition
{
    TMessageDefinition[] FindDefinitions();
}