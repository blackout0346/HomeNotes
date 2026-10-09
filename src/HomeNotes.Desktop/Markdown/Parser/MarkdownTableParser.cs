using System.Collections.Generic;
using System.Text.RegularExpressions;
using AvaloniaEdit.Document;
using HomeNotes.Desktop.Markdown.Enum;

namespace HomeNotes.Desktop.Markdown.Parser;

public class MarkdownTableParser
{
    private static readonly Regex TableSeparatorRegex =
        new(@"^\s*\|?\s*:?-+:?\s*(?:\|\s*:?-+:?\s*)+\|?\s*$", RegexOptions.Compiled);

    public static bool IsTableStart(
        TextDocument document,
        int lineNumber)
    {
        if (lineNumber >= document.LineCount)
            return false;

        var headerLine = document.GetLineByNumber(lineNumber);
        var headerText = document.GetText(headerLine);

        if (!headerText.Contains('|'))
            return false;

        var separatorLine = document.GetLineByNumber(lineNumber + 1);
        var separatorText = document.GetText(separatorLine);

        return TableSeparatorRegex.IsMatch(separatorText);
    }

    public static MarkdownTableBlock ParseTable(TextDocument document, ref int lineNumber)
    {
        int startLine = lineNumber;
        var headerLine = document.GetLineByNumber(lineNumber);
        var headers = SplitRow(document.GetText(headerLine));

        var separatorLine = document.GetLineByNumber(lineNumber + 1);
        var alignments = ParseAlignments(document.GetText(separatorLine), headers.Count);

        var rows = new List<List<string>>();
        int cursor = lineNumber + 2;
        while (cursor <= document.LineCount)
        {
            var rowText = document.GetText(document.GetLineByNumber(cursor));
            if (!IsTableRow(rowText, headers.Count))
                break;

            rows.Add(SplitRow(rowText));
            cursor++;
        }

        int endLine = cursor - 1;
        var lastLine = document.GetLineByNumber(endLine);
        lineNumber = cursor;

        return new MarkdownTableBlock
        {
            Headers = headers,
            Alignments = alignments,
            Rows = rows,
            StartOffset = headerLine.Offset,
            EndOffset = lastLine.EndOffset,
            StartLine = startLine,
            EndLine = endLine
        };
    }

    private static List<string> SplitRow(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("|")) trimmed = trimmed[1..];
        if (trimmed.EndsWith("|")) trimmed = trimmed[..^1];

        var cells = new List<string>();
        foreach (var cell in trimmed.Split('|'))
            cells.Add(cell.Trim());
        return cells;
    }

    private static List<MarkdownColumnAlignment> ParseAlignments(string separatorText, int columnCount)
    {
        var cells = SplitRow(separatorText);
        var result = new List<MarkdownColumnAlignment>();

        for (int i = 0; i < columnCount; i++)
        {
            var cell = i < cells.Count ? cells[i] : "-";
            bool left = cell.StartsWith(":");
            bool right = cell.EndsWith(":");

            result.Add(left && right ? MarkdownColumnAlignment.Center
                : right ? MarkdownColumnAlignment.Right
                : MarkdownColumnAlignment.Left);
        }

        return result;
    }
    private static bool IsTableRow(string text, int columnCount)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.Contains('|'))
            return false;

        return SplitRow(text).Count == columnCount;
    }
}