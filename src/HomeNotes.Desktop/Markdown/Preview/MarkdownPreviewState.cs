using System;
using System.Collections.Generic;
using HomeNotes.Desktop.Markdown.Parser;

namespace HomeNotes.Desktop.Markdown.Preview;

public class MarkdownPreviewState
{
    public IReadOnlyList<MarkdownBlock> Blocks { get; set; } = Array.Empty<MarkdownBlock>();
    public int CaretLine { get; set; } = -1;
    public bool IsCaretInsideBlock(MarkdownBlock block) => block.ContainsLine(CaretLine);
}