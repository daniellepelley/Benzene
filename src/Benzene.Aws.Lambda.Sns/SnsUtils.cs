namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Provides helper methods for reading SNS message attributes.
/// </summary>
public static class SnsUtils
{
    /// <summary>
    /// Gets a message attribute value from an SNS record by key.
    /// </summary>
    /// <param name="context">The SNS record context to read the attribute from.</param>
    /// <param name="key">The message attribute key to look up.</param>
    /// <returns>The attribute value, or null if the attribute isn't present.</returns>
    public static string GetFromAttributes(SnsRecordContext context, string key)
    {
        var check = context.SnsRecord.Sns?.MessageAttributes?.ContainsKey(key);

        if (!check.HasValue || !check.Value)
        {
            return null;
        }

        return context.SnsRecord.Sns.MessageAttributes[key].Value;
    }
}
