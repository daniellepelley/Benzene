namespace Benzene.Aws.Sns;

public static class SnsUtils
{
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
