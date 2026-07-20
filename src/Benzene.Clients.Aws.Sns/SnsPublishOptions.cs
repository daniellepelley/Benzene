namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Optional, opt-in SNS publish behaviors: FIFO ordering/deduplication and numeric attribute typing.
/// All fields default to disabled, so the publish behaves exactly as before unless configured.
/// </summary>
public class SnsPublishOptions
{
    /// <summary>
    /// The header whose value sets <see cref="Amazon.SimpleNotificationService.Model.PublishRequest.MessageGroupId"/>,
    /// required to publish to a FIFO (<c>.fifo</c>) topic. <c>null</c> publishes without a group id.
    /// </summary>
    public string? MessageGroupIdHeader { get; set; }

    /// <summary>
    /// The header whose value sets <see cref="Amazon.SimpleNotificationService.Model.PublishRequest.MessageDeduplicationId"/>,
    /// used by a FIFO topic for content-independent deduplication. <c>null</c> relies on the topic's
    /// content-based deduplication (if enabled).
    /// </summary>
    public string? MessageDeduplicationIdHeader { get; set; }

    /// <summary>
    /// When <c>true</c>, a forwarded header whose value parses as a number is published with SNS
    /// attribute <c>DataType = "Number"</c> instead of <c>"String"</c>, so numeric subscription filter
    /// policies match it. Defaults to <c>false</c> to avoid silently changing attribute types.
    /// </summary>
    public bool InferNumericAttributeTypes { get; set; }
}
