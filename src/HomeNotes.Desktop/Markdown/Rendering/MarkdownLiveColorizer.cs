using System;
using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

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
        private static readonly IBrush QuoteBrush = new SolidColorBrush(Color.Parse("#9AA0A6"));
        private static readonly IBrush LinkBrush = new SolidColorBrush(Color.Parse("#811FFF"));

        private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.*)$", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new(@"(\*\*|__)(.+?)\1", RegexOptions.Compiled);

        private static readonly Regex ItalicRegex = new(@"(?<!\*)\*(?!\*)([^*]+?)\*(?!\*)|(?<!_)_(?!_)([^_]+?)_(?!_)",
            RegexOptions.Compiled);

        private static readonly Regex InlineCodeRegex = new("`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex ImageRegex = new(@"!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex LinkRegex = new(@"(?<!!)\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex ListMarkerRegex = new(@"^(\s*)([-*+]|\d+\.)\s", RegexOptions.Compiled);
        private static readonly Regex CheckboxRegex = new(@"^(\s*-\s*\[[ xX]\])\s+", RegexOptions.Compiled);
        private static readonly Regex StrikethroughRegex = new(@"~~(.+?)~~", RegexOptions.Compiled);
        private static readonly Regex CodeBlockRegex = new(@"^\s*```.*$", RegexOptions.Compiled);

        // НОВЫЕ ПРАВИЛА
        private static readonly Regex
            EscapeRegex = new(@"\\([^\w\s])", RegexOptions.Compiled); // Экранирование (любой спецсимвол после \)

        private static readonly Regex TableSeparatorRegex =
            new(@"^\s*\|?\s*:?-+:?\s*(?:\|\s*:?-+:?\s*)+\|?\s*$", RegexOptions.Compiled); // Разделитель |---|

        private static readonly Regex
            HrRegex = new(@"^\s*([-*_])\s*(?:\1\s*){2,}$", RegexOptions.Compiled); // Горизонтальная линия ---

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
                        try
                        {
                            ChangeLinePart(start, end, action);
                        }
                        catch
                        {
                        }
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

                // ---- Экранированные символы (например, \#) ----
                foreach (Match m in EscapeRegex.Matches(text))
                {
                    int start = lineStart + m.Index;
                    // Прячем только слэш, сам символ оставляем обычным
                    SafeChangeLinePart(start, start + 1, ApplyMarkerStyle);
                }

                // ---- Горизонтальные линии (---) и разделители таблиц (|---|---|) ----
                if (HrRegex.IsMatch(text) || TableSeparatorRegex.IsMatch(text))
                {
                    // Мы не скрываем их, иначе структура ломается. Просто делаем всю строку тусклой
                    SafeChangeLinePart(lineStart, line.EndOffset,
                        el => el.TextRunProperties.SetForegroundBrush(DimBrush));
                    return;
                }

                // ---- Трубы (границы) таблиц ----
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '|')
                    {
                        // Проверяем, не экранирована ли труба ( \| )
                        if (i == 0 || text[i - 1] != '\\')
                        {
                            // Делаем тусклой, но не скрываем, чтобы таблица сохранила сетку
                            SafeChangeLinePart(lineStart + i, lineStart + i + 1,
                                el => el.TextRunProperties.SetForegroundBrush(DimBrush));
                        }
                    }
                }

                // ---- Заголовки ----
                var heading = HeadingRegex.Match(text);
                if (heading.Success)
                {
                    int hashLen = heading.Groups[1].Length;
                    SafeChangeLinePart(lineStart, lineStart + hashLen + 1, ApplyMarkerStyle);

                    int contentStart = lineStart + heading.Groups[2].Index;
                    double size = hashLen switch { 1 => 26, 2 => 23, 3 => 20, 4 => 18, 5 => 16, _ => 15 };

                    SafeChangeLinePart(contentStart, line.EndOffset, el =>
                    {
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Normal, FontWeight.Bold));
                        el.TextRunProperties.SetFontRenderingEmSize(size);
                        el.TextRunProperties.SetForegroundBrush(HeadingBrush);
                    });
                    return;
                }

                // ---- Блоки кода ----
                var codeBlock = CodeBlockRegex.Match(text);
                if (codeBlock.Success)
                {
                    SafeChangeLinePart(lineStart, line.EndOffset, ApplyMarkerStyle);
                    return;
                }

                // ---- Цитаты ----
                if (text.TrimStart().StartsWith(">"))
                {
                    SafeChangeLinePart(lineStart, line.EndOffset, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(QuoteBrush);
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Italic));
                    });
                }

                // ---- Чекбоксы и списки ----
                var cbMatch = CheckboxRegex.Match(text);
                if (cbMatch.Success)
                {
                    int markerStart = lineStart + cbMatch.Groups[1].Index;
                    int markerEnd = markerStart + cbMatch.Groups[1].Length;
                    SafeChangeLinePart(markerStart, markerEnd, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(AccentBrush);
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Normal, FontWeight.Bold));
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
                            el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily,
                                FontStyle.Normal, FontWeight.Bold));
                        });
                    }
                }

                // ---- Изображения ----
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
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Italic));
                    });
                    SafeChangeLinePart(textEnd, end, ApplyMarkerStyle);
                }

                // ---- Ссылки ----
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
                        el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Normal, FontWeight.Bold));
                    });
                    SafeChangeLinePart(textEnd, end, ApplyMarkerStyle);
                }

                // ---- Жирный ----
                foreach (Match m in BoldRegex.Matches(text))
                {
                    int markerLen = m.Groups[1].Length;
                    int start = lineStart + m.Index;
                    int contentStart = start + markerLen;
                    int contentEnd = contentStart + m.Groups[2].Length;
                    int end = contentEnd + markerLen;

                    SafeChangeLinePart(start, contentStart, ApplyMarkerStyle);
                    SafeChangeLinePart(contentStart, contentEnd,
                        el => el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Normal, FontWeight.Bold)));
                    SafeChangeLinePart(contentEnd, end, ApplyMarkerStyle);
                }

                // ---- Зачеркнутый текст ----
                foreach (Match m in StrikethroughRegex.Matches(text))
                {
                    int start = lineStart + m.Index;
                    int contentStart = start + 2;
                    int contentEnd = contentStart + m.Groups[1].Length;
                    int end = contentEnd + 2;

                    SafeChangeLinePart(start, contentStart, ApplyMarkerStyle);
                    SafeChangeLinePart(contentStart, contentEnd,
                        el => el.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough));
                    SafeChangeLinePart(contentEnd, end, ApplyMarkerStyle);
                }

                // ---- Курсив ----
                foreach (Match m in ItalicRegex.Matches(text))
                {
                    var group = m.Groups[1].Success ? m.Groups[1] : m.Groups[2];
                    int start = lineStart + m.Index;
                    int contentStart = lineStart + group.Index;
                    int contentEnd = contentStart + group.Length;
                    int end = start + m.Length;

                    SafeChangeLinePart(start, contentStart, ApplyMarkerStyle);
                    SafeChangeLinePart(contentStart, contentEnd,
                        el => el.TextRunProperties.SetTypeface(new Typeface(el.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Italic)));
                    SafeChangeLinePart(contentEnd, end, ApplyMarkerStyle);
                }

                // ---- Инлайн-код ----
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
                // Игнорируем ошибки парсинга, чтобы не ломать отрисовку экрана
            }
        }
    }
}