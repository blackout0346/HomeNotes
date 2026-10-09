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

                if (MarkdownTableParser.IsTableStart(document, lineNumber))
                {
                    blocks.Add(
                        MarkdownTableParser.ParseTable(document, ref lineNumber));

                    continue;
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
    }
}