namespace Benzene.Abstractions.MessageHandling;

public class CompositeMessageHandlersFinder : IMessageHandlersFinder
{
    private readonly IMessageHandlersFinder[] _inners;

    public CompositeMessageHandlersFinder(params IMessageHandlersFinder[] inners)
    {
        _inners = inners;
    }

    public IMessageHandlerDefinition[] FindDefinitions()
    {
        return _inners.SelectMany(x => x.FindDefinitions()).ToArray();
    }
}