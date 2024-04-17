using Benzene.CodeGen.Core;

namespace Benzene.CodeGen.Cli.Core.Commands.Build;

public static class LambdaNameParser
{
    public static string GetServiceName(string lambdaName)
    {
        return string.Join("", lambdaName
            .Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.ToLowerInvariant() != "func")
            .Select(x => new FormatString(x).Pascalcase()));
    }

    public static string GetNamespace(string lambdaName, string suffix)
    {
        return string.Join(".", lambdaName
                   .Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                   .Where(x => x.ToLowerInvariant() != "func")
                   .Select(x => new FormatString(x).Pascalcase()))
               + $".{suffix}";
    }
}
