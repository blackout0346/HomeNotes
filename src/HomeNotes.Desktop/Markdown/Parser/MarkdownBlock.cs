using System.Reflection.Metadata;

namespace HomeNotes.Desktop.Markdown.Parser;

public abstract class MarkdownBlock
{
    public int StartOffset{ get; init; }
    public int EndOffset { get; init; }
    public int StartLine{ get; init; }
    public int EndLine { get; init; }
    public int DocumentLength => EndOffset - StartOffset;
    public bool ContainsLine(int lineNumber) => lineNumber >= StartLine && lineNumber <= EndLine;

}