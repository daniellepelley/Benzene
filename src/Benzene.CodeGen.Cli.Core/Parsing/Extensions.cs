namespace Benzene.CodeGen.Cli.Core.Parsing;

public static class Extensions
{
    public static CommandArguments Parse(this ICommandParser source, string args)
    {
        return source.Parse(new CommandSplitter().Split(args));
    }

    public static string GetValue(this CommandArguments source, string key, string defaultValue = "")
    {
        // A bare value-less flag is stored as key -> null; fall back to the default in that case too,
        // not just when the key is absent (the return type is non-null and callers set string props).
        return source.Attributes.TryGetValue(key, out var value) && value != null ? value : NotNull(defaultValue);
    }

    private static string NotNull(string? value)
    {
        return value == null
            ? string.Empty
            : value;
    }
}
