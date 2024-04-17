namespace Benzene.CodeGen.Core.Writers;

public class LineWriter : ILineWriter
{
    private readonly List<string> _lines = new();
    private readonly Indent _indent = new();
    private readonly int _indentSize;

    public LineWriter(int indentSize = 4)
    {
        _indentSize = indentSize;
    }

    public void WriteLine(string line)
    {
        var space = string.Concat(Enumerable.Repeat(" ", _indent.Level * _indentSize));
        _lines.Add($"{space}{line}");
    }

    public void WriteLine(string line, int indents)
    {
        var space = string.Concat(Enumerable.Repeat(" ", indents * _indentSize));
        _lines.Add($"{space}{line}");
    }

    public void WriteLine()
    {
        _lines.Add(string.Empty);
    }

    public IDisposable StartIndent()
    {
        return new StartIndent(_indent);
    }

    public string[] GetLines()
    {
        return _lines.ToArray();
    }
}
