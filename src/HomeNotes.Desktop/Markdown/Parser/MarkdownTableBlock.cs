using System.Collections.Generic;
using HomeNotes.Desktop.Markdown.Enum;

namespace HomeNotes.Desktop.Markdown.Parser;

public class MarkdownTableBlock : MarkdownBlock
{
    public List<string> Headers { get; init; } = new();
    public List<MarkdownColumnAlignment> Alignments { get; init; } = new();
    public List<List<string>> Rows { get; init; } = new();
}