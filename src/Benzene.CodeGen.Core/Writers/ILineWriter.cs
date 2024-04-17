namespace Benzene.CodeGen.Core.Writers;

public interface ILineWriter
{
    public void WriteLine(string line);
    public void WriteLine();
    public IDisposable StartIndent();
    string[] GetLines();
}
