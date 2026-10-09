using System;
using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;

namespace HomeNotes.Desktop.Markdown.Rendering
{
    public class MarkdownLiveColorizer : DocumentColorizingTransformer
    {
        public int CaretOffset { get; set; } = -1;

        private static readonly IBrush DimBrush = new SolidColorBrush(Color.Parse("#5A5D63"));
        private static readonly IBrush HeadingBrush = new SolidColorBrush(Colors.White);
        private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#811FFF"));
        private static readonly IBrush CodeBrush = new SolidColorBrush(Color.Parse("#F2C078"));
        private static readonly IBrush CodeBackground = new SolidColorBrush(Color.Parse("#17181A"));
        private static readonly IBrush CodeMarkerActiveBrush = new SolidColorBrush(Color.Parse("#FFC600")); 
        private static readonly IBrush QuoteBrush = new SolidColorBrush(Color.Parse("#9AA0A6"));
        private static readonly IBrush LinkBrush = new SolidColorBrush(Color.Parse("#811FFF"));
        private static readonly IBrush HighlightBackground = new SolidColorBrush(Color.Parse("#F5D76E"));
        private static readonly IBrush HighlightForeground = new SolidColorBrush(Colors.Black);
        private static readonly IBrush CalloutBrush = new SolidColorBrush(Color.Parse("#58A6FF"));

        private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.*)$", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new(@"(\*\*|__)(.+?)\1", RegexOptions.Compiled);
        private static readonly Regex ItalicRegex = new(@"(?<!\*)\*(?!\*)([^*]+?)\*(?!\*)|(?<!_)_(?!_)([^_]+?)_(?!_)", RegexOptions.Compiled);
        private static readonly Regex InlineCodeRegex = new("`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex ImageRegex = new(@"!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled); 
        private static readonly Regex LinkRegex = new(@"(?<!!)\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled); 
        private static readonly Regex ListMarkerRegex = new(@"^(\s*)([-*+]|\d+\.)\s", RegexOptions.Compiled);
        private static readonly Regex CheckboxRegex = new(@"^(\s*-\s*\[[ xX]\])\s+", RegexOptions.Compiled); 
        private static readonly Regex StrikethroughRegex = new(@"~~(.+?)~~", RegexOptions.Compiled); 
        private static readonly Regex EscapeRegex = new(@"\\([^\w\s])", RegexOptions.Compiled); 
        private static readonly Regex TableSeparatorRegex = new(@"^\s*\|?\s*:?-+:?\s*(?:\|\s*:?-+:?\s*)+\|?\s*$", RegexOptions.Compiled); 
        private static readonly Regex HrRegex = new(@"^\s*([-*_])\s*(?:\1\s*){2,}$", RegexOptions.Compiled); 
        private static readonly Regex HighlightRegex = new(@"==(.+?)==", RegexOptions.Compiled);
        private static readonly Regex CalloutRegex = new(@"^\s*>\s*\[!(\w+)\](.*)$", RegexOptions.Compiled);

        protected override void ColorizeLine(DocumentLine line)
        {
            try
            {
                var text = CurrentContext.Document.GetText(line);
                if (string.IsNullOrEmpty(text)) return;

                int lineStart = line.Offset;
                bool isActiveLine = CaretOffset >= line.Offset && CaretOffset <= line.EndOffset;

                void SafeChangeLinePart(int start, int end, Action<VisualLineElement> action)
                {
                    if (start < end && start >= line.Offset && end <= line.EndOffset)
                    {
                        try { ChangeLinePart(start, end, action); } catch { }
                    }
                }

                void ApplyMarkerStyle(VisualLineElement el)
                {
                    if (isActiveLine)
                    {
                        el.TextRunProperties.SetForegroundBrush(DimBrush);
                    }
                    else
                    {
                        el.TextRunProperties.SetForegroundBrush(Brushes.Transparent);
                        el.TextRunProperties.SetFontRenderingEmSize(1);
                    }
                }

                

                bool inCodeBlock = false;
                for (int i = 1; i < line.LineNumber; i++)
                {
                    var prevLine = CurrentContext.Document.GetLineByNumber(i);
                    var prevText = CurrentContext.Document.GetText(prevLine).TrimStart();
                    if (prevText.StartsWith("```"))
                    {
                        inCodeBlock = !inCodeBlock;
                    }
                }

                // ---- Границы многострочных блоков кода ----
                if (text.TrimStart().StartsWith("```"))
                {
                    SafeChangeLinePart(lineStart, line.EndOffset, el =>
                    {
                        if (isActiveLine)
                        {
                            el.TextRunProperties.SetForegroundBrush(CodeMarkerActiveBrush); 
                            el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                        }
                        else
                        {
                            el.TextRunProperties.SetForegroundBrush(Brushes.Transparent);
                            el.TextRunProperties.SetFontRenderingEmSize(1);
                        }
                    });
                    return; 
                }

                if (inCodeBlock)
                {
                    SafeChangeLinePart(lineStart, line.EndOffset, el =>
                    {
                        el.TextRunProperties.SetTypeface(new Typeface("Cascadia Code,Consolas,monospace"));
                        el.TextRunProperties.SetForegroundBrush(CodeBrush);
                    });
                    return; 
                }

                foreach (Match m in EscapeRegex.Matches(text))
                {
                    int start = lineStart + m.Index;
                    SafeChangeLinePart(start, start + 1, ApplyMarkerStyle);
                }

                if (HrRegex.IsMatch(text) || TableSeparatorRegex.IsMatch(text))
                {
                    SafeChangeLinePart(lineStart, line.EndOffset, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
                    return; 
                }

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '|' && (i == 0 || text[i - 1] != '\\'))
                    {
                        SafeChangeLinePart(lineStart + i, lineStart + i + 1, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
                    }
                }

                var heading = HeadingRegex.Match(text);
                if (heading.Success)
                {
                    int hashLen = heading.Groups[1].Length;
                    SafeChangeLinePart(lineStart, lineStart + hashLen + 1, ApplyMarkerStyle);

                    int contentStart = lineStart + heading.Groups[2].Index;
                    double size = hashLen switch { 1 => 26, 2 => 23, 3 => 20, 4 => 18, 5 => 16, _ => 15 };

                    SafeChangeLinePart(contentStart, line.EndOffset, el =>
                    {
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                        el.TextRunProperties.SetFontRenderingEmSize(size);
                        el.TextRunProperties.SetForegroundBrush(HeadingBrush);
                    });
                    return;
                }
                
                var callout = CalloutRegex.Match(text);
                if (callout.Success)
                {
                    SafeChangeLinePart(lineStart, line.EndOffset, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(CalloutBrush);
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                    });
                }
                else if (text.TrimStart().StartsWith(">"))
                {
                    SafeChangeLinePart(lineStart, line.EndOffset, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(QuoteBrush);
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Italic));
                    });
                }

                var cbMatch = CheckboxRegex.Match(text);
                if (cbMatch.Success)
                {
                    int markerStart = lineStart + cbMatch.Groups[1].Index;
                    int markerEnd = markerStart + cbMatch.Groups[1].Length;
                    SafeChangeLinePart(markerStart, markerEnd, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(AccentBrush);
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                    });
                }
                else
                {
                    var listMatch = ListMarkerRegex.Match(text);
                    if (listMatch.Success)
                    {
                        int markerStart = lineStart + listMatch.Groups[2].Index;
                        int markerEnd = markerStart + listMatch.Groups[2].Length;
                        SafeChangeLinePart(markerStart, markerEnd, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush(AccentBrush);
                            el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                        });
                    }
                }

                foreach (Match m in ImageRegex.Matches(text))
                {
                    int start = lineStart + m.Index;
                    int textStart = start + 2; 
                    int textEnd = textStart + m.Groups[1].Length;
                    int end = start + m.Length;

                    SafeChangeLinePart(start, textStart, ApplyMarkerStyle);
                    SafeChangeLinePart(textStart, textEnd, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(LinkBrush);
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Italic));
                    });
                    SafeChangeLinePart(textEnd, end, ApplyMarkerStyle); 
                }

                foreach (Match m in LinkRegex.Matches(text))
                {
                    int start = lineStart + m.Index;
                    int textStart = start + 1; 
                    int textEnd = textStart + m.Groups[1].Length;
                    int end = start + m.Length;

                    SafeChangeLinePart(start, textStart, ApplyMarkerStyle);
                    SafeChangeLinePart(textStart, textEnd, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(LinkBrush);
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                    });
                    SafeChangeLinePart(textEnd, end, ApplyMarkerStyle); 
                }

                foreach (Match m in HighlightRegex.Matches(text))
                {
                    int start = lineStart + m.Index;
                    int contentStart = start + 2;
                    int contentEnd = contentStart + m.Groups[1].Length;
                    int end = contentEnd + 2;

                    SafeChangeLinePart(start, contentStart, ApplyMarkerStyle);
                    SafeChangeLinePart(contentStart, contentEnd, el =>
                    {
                        el.TextRunProperties.SetBackgroundBrush(HighlightBackground);
                        el.TextRunProperties.SetForegroundBrush(HighlightForeground);
                    });
                    SafeChangeLinePart(contentEnd, end, ApplyMarkerStyle);
                }

                foreach (Match m in BoldRegex.Matches(text))
                {
                    int markerLen = m.Groups[1].Length;
                    int start = lineStart + m.Index;
                    int contentStart = start + markerLen;
                    int contentEnd = contentStart + m.Groups[2].Length;
                    int end = contentEnd + markerLen;

                    SafeChangeLinePart(start, contentStart, ApplyMarkerStyle);
                    SafeChangeLinePart(contentStart, contentEnd, el => el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold)));
                    SafeChangeLinePart(contentEnd, end, ApplyMarkerStyle);
                }

                foreach (Match m in StrikethroughRegex.Matches(text))
                {
                    int start = lineStart + m.Index;
                    int contentStart = start + 2;
                    int contentEnd = contentStart + m.Groups[1].Length;
                    int end = contentEnd + 2;

                    SafeChangeLinePart(start, contentStart, ApplyMarkerStyle);
                    SafeChangeLinePart(contentStart, contentEnd, el => el.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough));
                    SafeChangeLinePart(contentEnd, end, ApplyMarkerStyle);
                }

                foreach (Match m in ItalicRegex.Matches(text))
                {
                    var group = m.Groups[1].Success ? m.Groups[1] : m.Groups[2];
                    int start = lineStart + m.Index;
                    int contentStart = lineStart + group.Index;
                    int contentEnd = contentStart + group.Length;
                    int end = start + m.Length;

                    SafeChangeLinePart(start, contentStart, ApplyMarkerStyle);
                    SafeChangeLinePart(contentStart, contentEnd, el => el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Italic)));
                    SafeChangeLinePart(contentEnd, end, ApplyMarkerStyle);
                }

                foreach (Match m in InlineCodeRegex.Matches(text))
                {
                    int start = lineStart + m.Index;
                    int contentStart = start + 1;
                    int contentEnd = contentStart + m.Groups[1].Length;
                    int end = contentEnd + 1;

                    SafeChangeLinePart(start, contentStart, ApplyMarkerStyle);
                    SafeChangeLinePart(contentStart, contentEnd, el =>
                    {
                        el.TextRunProperties.SetTypeface(new Typeface("Cascadia Code,Consolas,monospace"));
                        el.TextRunProperties.SetForegroundBrush(CodeBrush);
                        el.TextRunProperties.SetBackgroundBrush(CodeBackground);
                    });
                    SafeChangeLinePart(contentEnd, end, ApplyMarkerStyle);
                }
            }
            catch
            {
            }
        }
    }
}