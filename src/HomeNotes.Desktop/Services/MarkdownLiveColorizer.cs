using System.Text.RegularExpressions;
using Avalonia.Media;

using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;


namespace HomeNotes.Desktop.Services
{
    // DocumentColorizingTransformer вызывается автоматически на каждую видимую строку
    // при любом изменении документа — отдельный debounce/таймер не нужен,
    // AvaloniaEdit сам перерисовывает только то, что видно на экране.
    public class MarkdownLiveColorizer : DocumentColorizingTransformer
    {
        private static readonly IBrush DimBrush = new SolidColorBrush(Color.Parse("#5A5D63"));   // тускнеющий синтаксис (##, **, `` и т.д.)
        private static readonly IBrush HeadingBrush = new SolidColorBrush(Colors.White);
        private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#811FFF")); // маркеры списков, акценты
        private static readonly IBrush CodeBrush = new SolidColorBrush(Color.Parse("#F2C078"));
        private static readonly IBrush CodeBackground = new SolidColorBrush(Color.Parse("#17181A"));
        private static readonly IBrush QuoteBrush = new SolidColorBrush(Color.Parse("#9AA0A6"));
        private static readonly IBrush LinkBrush = new SolidColorBrush(Color.Parse("#811FFF"));

        private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.*)$", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new(@"(\*\*|__)(.+?)\1", RegexOptions.Compiled);
        private static readonly Regex ItalicRegex = new(@"(?<!\*)\*(?!\*)([^*]+?)\*(?!\*)|(?<!_)_(?!_)([^_]+?)_(?!_)", RegexOptions.Compiled);
        private static readonly Regex InlineCodeRegex = new("`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex ListMarkerRegex = new(@"^(\s*)([-*+]|\d+\.)\s", RegexOptions.Compiled);

        protected override void ColorizeLine(DocumentLine line)
        {
            var text = CurrentContext.Document.GetText(line);
            if (string.IsNullOrEmpty(text)) return;

            int lineStart = line.Offset;

            // ---- Заголовок — крупнее, жирный, а сами "#" тускнеют. ----
            // Если строка — заголовок, остальные инлайн-стили внутри неё не считаем
            // (в markdown заголовок обычно не комбинируют с блочными элементами построчно).
            var heading = HeadingRegex.Match(text);
            if (heading.Success)
            {
                int hashLen = heading.Groups[1].Length;

                ChangeLinePart(lineStart, lineStart + hashLen + 1, el =>
                    el.TextRunProperties.SetForegroundBrush(DimBrush));

                int contentStart = lineStart + heading.Groups[2].Index;
                double size = hashLen switch
                {
                    1 => 26,
                    2 => 23,
                    3 => 20,
                    4 => 18,
                    5 => 16,
                    _ => 15
                };

                ChangeLinePart(contentStart, line.EndOffset, el =>
                {
                    el.TextRunProperties.SetTypeface(new Typeface(
                        el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                    el.TextRunProperties.SetFontRenderingEmSize(size);
                    el.TextRunProperties.SetForegroundBrush(HeadingBrush);
                });
                return;
            }

            // ---- Цитата "> текст" — вся строка курсивом и приглушённым цветом ----
            var trimmed = text.TrimStart();
            if (trimmed.StartsWith(">"))
            {
                ChangeLinePart(lineStart, line.EndOffset, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(QuoteBrush);
                    el.TextRunProperties.SetTypeface(new Typeface(
                        el.TextRunProperties.Typeface.FontFamily, FontStyle.Italic));
                });
                // цитата ещё может содержать **жирный**/`код` внутри — не возвращаемся,
                // даём остальным правилам докрасить содержимое поверх
            }

            // ---- Маркер списка "- " / "1. " — акцентным цветом и жирным ----
            var listMatch = ListMarkerRegex.Match(text);
            if (listMatch.Success)
            {
                int markerStart = lineStart + listMatch.Groups[2].Index;
                int markerEnd = markerStart + listMatch.Groups[2].Length;
                ChangeLinePart(markerStart, markerEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(AccentBrush);
                    el.TextRunProperties.SetTypeface(new Typeface(
                        el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                });
            }

            // ---- Жирный **текст** / __текст__ ----
            foreach (Match m in BoldRegex.Matches(text))
            {
                int markerLen = m.Groups[1].Length;
                int start = lineStart + m.Index;
                int contentStart = start + markerLen;
                int contentEnd = contentStart + m.Groups[2].Length;
                int end = contentEnd + markerLen;

                ChangeLinePart(start, contentStart, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
                ChangeLinePart(contentStart, contentEnd, el => el.TextRunProperties.SetTypeface(
                    new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold)));
                ChangeLinePart(contentEnd, end, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
            }

            // ---- Курсив *текст* / _текст_ ----
            foreach (Match m in ItalicRegex.Matches(text))
            {
                var group = m.Groups[1].Success ? m.Groups[1] : m.Groups[2];
                int start = lineStart + m.Index;
                int contentStart = lineStart + group.Index;
                int contentEnd = contentStart + group.Length;
                int end = start + m.Length;

                ChangeLinePart(start, contentStart, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
                ChangeLinePart(contentStart, contentEnd, el => el.TextRunProperties.SetTypeface(
                    new Typeface(el.TextRunProperties.Typeface.FontFamily, FontStyle.Italic)));
                ChangeLinePart(contentEnd, end, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
            }

            // ---- Инлайн-код `код` ----
            foreach (Match m in InlineCodeRegex.Matches(text))
            {
                int start = lineStart + m.Index;
                int contentStart = start + 1;
                int contentEnd = contentStart + m.Groups[1].Length;
                int end = contentEnd + 1;

                ChangeLinePart(start, contentStart, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
                ChangeLinePart(contentStart, contentEnd, el =>
                {
                    el.TextRunProperties.SetTypeface(new Typeface("Cascadia Code,Consolas,monospace"));
                    el.TextRunProperties.SetForegroundBrush(CodeBrush);
                    el.TextRunProperties.SetBackgroundBrush(CodeBackground);
                });
                ChangeLinePart(contentEnd, end, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
            }

            // ---- Ссылка [текст](url) — текст акцентным цветом с подчёркиванием, url тускнеет ----
            foreach (Match m in LinkRegex.Matches(text))
            {
                int start = lineStart + m.Index;
                int textStart = start + 1;
                int textEnd = textStart + m.Groups[1].Length;
                int end = start + m.Length;

                ChangeLinePart(start, textStart, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
                ChangeLinePart(textStart, textEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(LinkBrush);
                    el.TextRunProperties.SetTypeface(new Typeface(
                        el.TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold));
                });
                ChangeLinePart(textEnd, end, el => el.TextRunProperties.SetForegroundBrush(DimBrush));
            }
        }
    }
}