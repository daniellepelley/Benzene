namespace Benzene.CodeGen.Core.Writers;

public class StartIndent : IDisposable
{
    private readonly Indent _indent;

    public StartIndent(Indent indent)
    {
        indent.Level++;
        _indent = indent;
    }

    public void Dispose()
    {
        _indent.Level--;
    }
}