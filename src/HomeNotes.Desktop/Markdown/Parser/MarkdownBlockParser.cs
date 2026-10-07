using System.Collections.Generic;
using System.Text.RegularExpressions;
using AvaloniaEdit.Document;
using HomeNotes.Desktop.Markdown.Enum;

namespace HomeNotes.Desktop.Markdown.Parser
{

    public static class MarkdownBlockParser
    {
        private static readonly Regex ImageRegex = new(@"!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex WikiImageRegex = new(@"!\[\[([^\]|]+)(?:\|[^\]]*)?\]\]", RegexOptions.Compiled);
        private static readonly Regex TableSeparatorRegex =
            new(@"^\s*\|?\s*:?-+:?\s*(?:\|\s*:?-+:?\s*)+\|?\s*$", RegexOptions.Compiled);
        private static readonly Regex CodeBlockRegex = new(@"^\s*```(.*)$", RegexOptions.Compiled);
        public static List<MarkdownBlock> Parse(TextDocument document)
        {
            var blocks = new List<MarkdownBlock>();
            int lineNumber = 1;
            
            bool inCodeBlock = false;
            int codeBlockStartLine = -1;
            int codeBlockStartOffset = -1;
            string codeLanguage = string.Empty;

            while (lineNumber <= document.LineCount)
            {
                var line = document.GetLineByNumber(lineNumber);
                var text = document.GetText(line);

            
                var codeMatch = CodeBlockRegex.Match(text);
                if (codeMatch.Success)
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlockStartLine = lineNumber;
                        codeBlockStartOffset = line.Offset;
                        codeLanguage = codeMatch.Groups[1].Value.Trim();
                    }
                    else
                    {
                        inCodeBlock = false;
                        blocks.Add(new MarkdownCodeBlock
                        {
                            Language = codeLanguage,
                            StartOffset = codeBlockStartOffset,
                            EndOffset = line.EndOffset,
                            StartLine = codeBlockStartLine,
                            EndLine = lineNumber
                        });
                    }
                    lineNumber++;
                    continue;
                }

    
                if (inCodeBlock)
                {
                    lineNumber++;
                    continue;
                }

              
                var imageMatch = ImageRegex.Match(text);
                if (imageMatch.Success)
                {
                    blocks.Add(new MarkdownImageBlock
                    {
                        AltText = imageMatch.Groups[1].Value,
                        Url = imageMatch.Groups[2].Value,
                        StartOffset = line.Offset + imageMatch.Index,
                        EndOffset = line.Offset + imageMatch.Index + imageMatch.Length,
                        StartLine = lineNumber,
                        EndLine = lineNumber
                    });
                }

          
                var wikiImageMatch = WikiImageRegex.Match(text);
                if (wikiImageMatch.Success)
                {
                    var fileName = wikiImageMatch.Groups[1].Value;
                    blocks.Add(new MarkdownImageBlock
                    {
                        AltText = fileName,
                        Url = fileName,
                        StartOffset = line.Offset + wikiImageMatch.Index,
                        EndOffset = line.Offset + wikiImageMatch.Index + wikiImageMatch.Length,
                        StartLine = lineNumber,
                        EndLine = lineNumber
                    });
                }

              
                if (lineNumber < document.LineCount && text.Contains('|'))
                {
                    var nextLine = document.GetLineByNumber(lineNumber + 1);
                    if (TableSeparatorRegex.IsMatch(document.GetText(nextLine)))
                    {
                        blocks.Add(ParseTable(document, ref lineNumber));
                        continue; 
                    }
                }

                lineNumber++;
            }

           
            if (inCodeBlock)
            {
                var lastLine = document.GetLineByNumber(document.LineCount);
                blocks.Add(new MarkdownCodeBlock
                {
                    Language = codeLanguage,
                    StartOffset = codeBlockStartOffset,
                    EndOffset = lastLine.EndOffset,
                    StartLine = codeBlockStartLine,
                    EndLine = document.LineCount
                });
            }

            return blocks;
        }

        private static MarkdownTableBlock ParseTable(TextDocument document, ref int lineNumber)
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
                if (!rowText.Contains('|') || string.IsNullOrWhiteSpace(rowText)) break;

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
    }
}