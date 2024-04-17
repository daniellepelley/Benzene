namespace Benzene.CodeGen.Cli.Core.Parsing
{
    public class CommandParser : ICommandParser
    {
        private readonly AttributesParser _attributesParser = new();

        public CommandArguments Parse(string[] args)
        {
            return new CommandArguments
            {
                Name = args.ElementAt(0),
                Value = GetValue(args),
                Attributes = _attributesParser.Parse(args)
            };
        }

        private static string? GetValue(string[] args)
        {
            var value = args.ElementAtOrDefault(1);
            if (value != null && !value.StartsWith("-"))
            {
                return value;
            }
            return null;
        }
    }
}