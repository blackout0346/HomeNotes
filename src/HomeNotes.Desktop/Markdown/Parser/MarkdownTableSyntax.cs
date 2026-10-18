using System;
using System.Collections.Generic;

namespace HomeNotes.Desktop.Markdown.Parser;

/// <summary>
/// Shared, escape-aware primitives for Markdown table rows.
/// </summary>
internal static class MarkdownTableSyntax
{
    public static List<int> GetPipeIndices(string text)
    {
        var indices = new List<int>();

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '|' && (index == 0 || text[index - 1] != '\\'))
                indices.Add(index);
        }

        return indices;
    }

    public static List<string> SplitRow(string text)
    {
        var row = text.Trim();
        var pipes = GetPipeIndices(row);
        if (pipes.Count == 0)
            return new List<string> { row };

        var firstContentOffset = pipes[0] == 0 ? 1 : 0;
        var lastContentOffset = pipes[^1] == row.Length - 1 ? pipes[^1] : row.Length;
        var cells = new List<string>();
        var cellStart = firstContentOffset;

        foreach (var pipeOffset in pipes)
        {
            if (pipeOffset < cellStart || pipeOffset >= lastContentOffset)
                continue;

            cells.Add(row[cellStart..pipeOffset].Trim());
            cellStart = pipeOffset + 1;
        }

        if (cellStart <= lastContentOffset)
            cells.Add(row[cellStart..lastContentOffset].Trim());

        return cells;
    }

    public static string NormalizeCellContent(string content) =>
        content.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ');
}
