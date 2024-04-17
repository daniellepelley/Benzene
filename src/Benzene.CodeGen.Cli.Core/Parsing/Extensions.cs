namespace Benzene.CodeGen.Cli.Core.Parsing;

public static class Extensions
{
    public static CommandArguments Parse(this ICommandParser source, string args)
    {
        return source.Parse(new CommandSplitter().Split(args));
    }

    public static string GetValue(this CommandArguments source, string key, string defaultValue = "")
    {
        return source.Attributes.TryGetValue(key, out string value) ? value : NotNull(defaultValue);
    }

    private static string NotNull(string? value)
    {
        return value == null
            ? string.Empty
            : value;
    }
}
