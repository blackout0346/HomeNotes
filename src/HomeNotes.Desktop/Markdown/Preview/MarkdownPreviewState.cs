using System;
using System.Collections.Generic;
using HomeNotes.Desktop.Markdown.Parser;

namespace HomeNotes.Desktop.Markdown.Preview;

public enum TableAddTarget
{
    None,
    Row,
    Column
}

public class MarkdownPreviewState
{
    public IReadOnlyList<MarkdownBlock> Blocks { get; set; } = Array.Empty<MarkdownBlock>();
    public int CaretLine { get; set; } = -1;

    public MarkdownTableBlock? HoveredTable { get; set; }
    public TableAddTarget HoveredTableTarget { get; set; }
    public bool IsHoveringTableButton { get; set; }

    public bool IsCaretInsideBlock(MarkdownBlock block)
    {
        if (block is MarkdownImageBlock or MarkdownTableBlock)
            return false;

        return block.ContainsLine(CaretLine);
    }
}
