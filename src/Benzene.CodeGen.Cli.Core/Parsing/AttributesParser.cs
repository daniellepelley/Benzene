namespace Benzene.CodeGen.Cli.Core.Parsing;

public class AttributesParser : IAttributesParser
{
    public IDictionary<string, string?> Parse(string[] args)
    {
        var dictionary = new Dictionary<string, string?>();
        var i = 0;
        while (i < args.Length)
        {
            var key = args.ElementAt(i);
            var value = args.ElementAtOrDefault(i + 1);

            if (!key.StartsWith("-"))
            {
                i++;
            }
            else if (value != null && value.StartsWith("-"))
            {
                dictionary.Add(CleanKey(key), null);
                i += 1;
            }
            else
            {
                dictionary.Add(CleanKey(key), value);
                i += 2;
            }
        }
        return dictionary;
    }

    private string CleanKey(string key)
    {
        return key.StartsWith("-")
            ? CleanKey(key.Substring(1, key.Length - 1))
            : key;
    }
}
