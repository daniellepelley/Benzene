namespace Benzene.CodeGen.Terraform;

public class TerraformLambdaSettings
{
    public string Name { get; set; }
    public string EntryPoint { get; set; }
    public int Timeout { get; set; } = 30;
    public int MemorySize { get; set; } = 512;
    public int ReservedConcurrentExecutions = 3;
    public string Runtime { get; set; } = "dotnet6";
    public string Domain { get; set; }
    public string SubDomain { get; set; }
    public IDictionary<string, string[]> TopicsMap { get; set; }
    public TerraformEventBridgeRuleSettings EventBridge { get; set; }

    /// <summary>
    /// The Terraform expression for the Lambda's VPC subnet ids. Null (the default) emits no
    /// <c>vpc_config</c> block - the Lambda runs outside a VPC. Set it to place the Lambda in a VPC,
    /// e.g. <c>"var.subnet_ids"</c> or <c>"data.terraform_remote_state.network.outputs.private_subnet_ids"</c>.
    /// (Was a hard-coded <c>data.terraform_remote_state.practice_suite.outputs.private_subnet_ids</c>.)
    /// </summary>
    public string? SubnetIdsExpression { get; set; }

    /// <summary>
    /// The attributes listed in the Lambda's <c>lifecycle.ignore_changes</c>. Defaults to a generic
    /// set; add deployment-specific entries here (e.g. <c>tags["AutoTag_Creator"]</c> for an external
    /// auto-tagging tool). (Previously the AutoTag entries were hard-coded.)
    /// </summary>
    public IReadOnlyCollection<string> IgnoredChanges { get; set; } = new[] { "filename", "environment", "layers" };
}
