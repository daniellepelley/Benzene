namespace Benzene.CodeGen.Terraform;

public class TerraformEventBridgeRuleSettings
{
    public string LambdaName { get; set; }

    /// <summary>Optional event bus name. When null the rule targets the account's default bus.</summary>
    public string EventBusName { get; set; }

    /// <summary>Optional <c>source</c> filter for the event pattern. When null only <c>detail-type</c> is matched.</summary>
    public string[] Sources { get; set; }

    /// <summary>
    /// The Benzene message topics the Lambda consumes — matched verbatim against <c>detail-type</c>
    /// (see Benzene.Aws.Lambda.EventBridge). Populate directly, or via the
    /// <see cref="TerraformEventBridgeRuleBuilderExtensions"/> overloads that discover them from
    /// <c>[Message]</c> handler definitions.
    /// </summary>
    public string[] Topics { get; set; }
}
