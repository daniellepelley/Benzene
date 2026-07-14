using System.Reflection;
using Benzene.Abstractions.MessageHandlers;
using Benzene.CodeGen.Core;
using Benzene.Core.MessageHandlers;

namespace Benzene.CodeGen.Terraform;

public static class TerraformEventBridgeRuleBuilderExtensions
{
    public static ICodeFile[] BuildCodeFiles(this ICodeBuilder<TerraformEventBridgeRuleSettings> source,
        TerraformEventBridgeRuleSettings settings, params Assembly[] assemblies)
    {
        return source.BuildCodeFiles(settings, new ReflectionMessageHandlersFinder(assemblies).FindDefinitions());
    }

    public static ICodeFile[] BuildCodeFiles(this ICodeBuilder<TerraformEventBridgeRuleSettings> source,
        TerraformEventBridgeRuleSettings settings, params Type[] types)
    {
        return source.BuildCodeFiles(settings, new ReflectionMessageHandlersFinder(types).FindDefinitions());
    }

    public static ICodeFile[] BuildCodeFiles(this ICodeBuilder<TerraformEventBridgeRuleSettings> source,
        TerraformEventBridgeRuleSettings settings, IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        // Versions never reach the wire — every version of a topic shares one detail-type — so
        // definitions collapse to distinct topic ids. Ordinal sort keeps the output diff-stable.
        settings.Topics = messageHandlerDefinitions
            .Select(x => x.Topic.Id)
            .Distinct()
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return source.BuildCodeFiles(settings);
    }
}
