namespace Benzene.CodeGen.Core.Writers;

public static class LineWriterExtensions
{
    public static void WriteLines(this ILineWriter writer, string[] lines)
    {
        foreach (var line in lines)
        {
            writer.WriteLine(line);
        }
    }
}
