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
                        if (args.ElementAtOrDefault(i) == '\"')
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
        list.Add(string.Join("", currentWord));

        return list.ToArray();
    }
}