namespace Benzene.CodeGen.Terraform;

public class TerraformLambdaEventBusPermissionsSettings
{
    public string LambdaName { get; set; }
    public IDictionary<string, string[]> TopicsMap { get; set; }

    /// <summary>
    /// The name of the Terraform remote state that exposes the SNS topic ARNs as outputs (referenced
    /// as <c>data.terraform_remote_state.{SnsRemoteStateName}.outputs.{topic}</c>). Defaults to
    /// <c>"sns"</c>; set it to match your cross-stack layout.
    /// </summary>
    public string SnsRemoteStateName { get; set; } = "sns";
}
