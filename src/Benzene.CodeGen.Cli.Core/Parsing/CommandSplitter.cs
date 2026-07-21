namespace Benzene.CodeGen.Cli.Core.Parsing;

public class CommandSplitter : ICommandSplitter
{
    public string[] Split(string args)
    {
        var list = new List<string>();
        var currentWord = new List<string>();
        var i = 0;
        while (i < args.Length)
        {
            switch (args[i])
            {
                case ' ':
                    if (currentWord.Any())
                    {
                        list.Add(string.Join("", currentWord));
                        currentWord.Clear();
                    }
                    break;
                case '\"':
                    while (i < args.Length)
                    {
                        i++;
                        // End of string with no closing quote: flush what we have rather than
                        // reading past the end of the array (was an IndexOutOfRangeException).
                        if (i >= args.Length || args[i] == '\"')
                        {
                            list.Add(string.Join("", currentWord));
                            currentWord.Clear();
                            break;
                        }

                        currentWord.Add(args[i].ToString());
                    }
                    break;
                default:
                    currentWord.Add(args[i].ToString());
                    break;
            }

            i++;

        }

        // Only flush a final word when there is one; a quoted final argument (or a trailing space)
        // already flushed and cleared it, so an unconditional Add would append a spurious "" token.
        if (currentWord.Any())
        {
            list.Add(string.Join("", currentWord));
        }

        return list.ToArray();
    }
}