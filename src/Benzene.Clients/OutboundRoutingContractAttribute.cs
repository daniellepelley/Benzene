namespace Benzene.Clients;

/// <summary>
/// Marks a type whose <c>public static string[] RequiredTopics</c> field enumerates the outbound
/// topics a generated client requires; <see cref="ValidateOutboundRoutingExtensions.ValidateOutboundRouting"/>
/// only inspects types carrying this attribute. Emitted by <c>Benzene.CodeGen.Client</c> onto its
/// generated <c>{Service}ServiceClientRouting</c> classes; a hand-rolled routing holder must carry
/// it too to be discovered.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OutboundRoutingContractAttribute : Attribute
{
}
