using Benzene.CodeGen.Cli.Core;

namespace Benzene.CodeGen.Cli;

class Program
{
    static async Task Main(string[] args)
    {
        var consoleApplication = new ConsoleApplication();
        if (args.Length == 0)
        {
            do
            {
                var stringArgs = Console.ReadLine();

                try
                {
                    await consoleApplication.ExecuteAsync(stringArgs);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            } while (true);
        }

        await consoleApplication.ExecuteAsync(args);
    }
}
