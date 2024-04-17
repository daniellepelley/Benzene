using Benzene.CodeGen.Core.Writers;

namespace Benzene.CodeGen.Core;

public class DocumentMerger
{
    private readonly Func<string, bool> _isStartLine;
    private readonly Func<string, bool> _isEndLine;
    private bool _isAppending;
    private bool _hasAppended;

    public DocumentMerger(Func<string, bool> isStartLine, Func<string, bool> isEndLine)
    {
        _isEndLine = isEndLine;
        _isStartLine = isStartLine;
    }
    
    public string[] Merge(string[] lines, string[] newLines) 
    {
        _isAppending = false;
        _hasAppended = false;

        var lineWriter = new LineWriter();
        foreach (var line in lines)
        {
            if (!_isAppending)
            {
                IsAppending(newLines, line, lineWriter);
            }
            else
            {
                if (_isEndLine(line))
                {
                    IsEndLine(lineWriter, line);
                }

            }
        }

        if (!_hasAppended)
        {
            HasAppended(newLines, lineWriter);
        }

        return lineWriter.GetLines();
    }

    private static void HasAppended(IEnumerable<string> newLines, ILineWriter lineWriter)
    {
        lineWriter.WriteLine();
        foreach (var newLine in newLines)
        {
            lineWriter.WriteLine(newLine);
        }
    }

    private void IsEndLine(ILineWriter lineWriter, string line)
    {
        if (lineWriter.GetLines().LastOrDefault() != string.Empty)
        {
            lineWriter.WriteLine();
        }

        lineWriter.WriteLine(line);
        _isAppending = false;
    }

    private void IsAppending(IEnumerable<string> newLines, string line, ILineWriter lineWriter)
    {
        if (_isStartLine(line))
        {
            foreach (var newLine in newLines)
            {
                lineWriter.WriteLine(newLine);
            }
            _isAppending = true;
            _hasAppended = true;
        }
        else
        {
            lineWriter.WriteLine(line);
        }
    }
}
