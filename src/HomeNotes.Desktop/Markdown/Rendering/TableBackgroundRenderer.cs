using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Preview;

namespace HomeNotes.Desktop.Markdown.Rendering
{
    public class TableBackgroundRenderer : IBackgroundRenderer
    {
        private readonly MarkdownPreviewState _state;

        private static readonly IBrush TableBgBrush = new SolidColorBrush(Color.Parse("#1C1E24"));
        private static readonly IBrush HeaderBgBrush = new SolidColorBrush(Color.Parse("#26282E"));
        private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#3C3F46")), 1);

        private static readonly IBrush PlusBtnBgBrush = new SolidColorBrush(Color.Parse("#22252B"));
        private static readonly IBrush PlusBtnHoverBgBrush = new SolidColorBrush(Color.Parse("#323642"));
        private static readonly IPen PlusBtnHoverBorderPen = new Pen(new SolidColorBrush(Color.Parse("#811FFF")), 1);
        private static readonly IPen PlusIconPen = new Pen(new SolidColorBrush(Color.Parse("#A0A5B0")), 1.8, lineCap: PenLineCap.Round);
        private static readonly IPen PlusIconHoverPen = new Pen(Brushes.White, 1.8, lineCap: PenLineCap.Round);

        public KnownLayer Layer => KnownLayer.Background;

        public TableBackgroundRenderer(MarkdownPreviewState state)
        {
            _state = state;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            var document = textView.Document;
            if (document == null || !textView.VisualLinesValid)
                return;

            double scrollX = textView.ScrollOffset.X;
            double scrollY = textView.ScrollOffset.Y;

            foreach (var table in _state.Blocks.OfType<MarkdownTableBlock>())
            {
                if (!TryGetTableGeometry(
                        textView,
                        table,
                        scrollX,
                        scrollY,
                        out var tableRect,
                        out var headerRect,
                        out var pipeXPositions))
                {
                    continue;
                }

                if (tableRect.Bottom < -40 || tableRect.Top > textView.Bounds.Height + 40 || tableRect.Width <= 0)
                    continue;

                // 1. Общий фон таблицы и фон шапки
                drawingContext.DrawRectangle(TableBgBrush, null, tableRect, 6, 6);
                drawingContext.DrawRectangle(HeaderBgBrush, null, headerRect, 6, 6);

                // 2. Горизонтальная линия под шапкой
                drawingContext.DrawLine(BorderPen, new Point(tableRect.Left, headerRect.Bottom), new Point(tableRect.Right, headerRect.Bottom));

                // 3. Горизонтальные линии между строками данных
                for (int lineNum = table.StartLine + 3; lineNum <= table.EndLine; lineNum++)
                {
                    double rowTop = textView.GetVisualTopByDocumentLine(lineNum) - scrollY;
                    drawingContext.DrawLine(BorderPen, new Point(tableRect.Left, rowTop), new Point(tableRect.Right, rowTop));
                }

                // 4. Вертикальные линии между столбцами
                for (int i = 1; i < pipeXPositions.Count - 1; i++)
                {
                    double x = pipeXPositions[i];
                    drawingContext.DrawLine(BorderPen, new Point(x, tableRect.Top), new Point(x, tableRect.Bottom));
                }

                // 5. Внешняя рамка таблицы
                drawingContext.DrawRectangle(null, BorderPen, tableRect, 6, 6);

                // 6. Кнопки '+' справа и снизу при наведении на таблицу
                if (ReferenceEquals(_state.HoveredTable, table))
                {
                    var rightBtnRect = GetAddColumnButtonRect(tableRect);
                    var bottomBtnRect = GetAddRowButtonRect(tableRect);

                    DrawPlusButton(drawingContext, rightBtnRect, _state.IsHoveringAddColumn);
                    DrawPlusButton(drawingContext, bottomBtnRect, _state.IsHoveringAddRow);
                }
            }
        }

        private static void DrawPlusButton(DrawingContext context, Rect rect, bool isHovered)
        {
            var bg = isHovered ? PlusBtnHoverBgBrush : PlusBtnBgBrush;
            var border = isHovered ? PlusBtnHoverBorderPen : BorderPen;
            var iconPen = isHovered ? PlusIconHoverPen : PlusIconPen;

            context.DrawRectangle(bg, border, rect, 5, 5);

            var center = rect.Center;
            const double halfSize = 5.0;
            context.DrawLine(iconPen, new Point(center.X - halfSize, center.Y), new Point(center.X + halfSize, center.Y));
            context.DrawLine(iconPen, new Point(center.X, center.Y - halfSize), new Point(center.X, center.Y + halfSize));
        }

        public bool HitTestTableHover(
            TextView textView,
            Point pointerInTextView,
            out MarkdownTableBlock? hoveredTable,
            out bool isHoveringAddCol,
            out bool isHoveringAddRow)
        {
            hoveredTable = null;
            isHoveringAddCol = false;
            isHoveringAddRow = false;

            if (textView.Document == null || !textView.VisualLinesValid)
                return false;

            double scrollX = textView.ScrollOffset.X;
            double scrollY = textView.ScrollOffset.Y;

            foreach (var table in _state.Blocks.OfType<MarkdownTableBlock>())
            {
                if (!TryGetTableGeometry(textView, table, scrollX, scrollY, out var tableRect, out _, out _))
                    continue;

                var rightBtnRect = GetAddColumnButtonRect(tableRect);
                var bottomBtnRect = GetAddRowButtonRect(tableRect);
                var hoverZone = tableRect.Inflate(new Thickness(4, 4, 38, 38));

                if (hoverZone.Contains(pointerInTextView) ||
                    rightBtnRect.Contains(pointerInTextView) ||
                    bottomBtnRect.Contains(pointerInTextView))
                {
                    hoveredTable = table;
                    isHoveringAddCol = rightBtnRect.Contains(pointerInTextView);
                    isHoveringAddRow = bottomBtnRect.Contains(pointerInTextView);
                    return true;
                }
            }

            return false;
        }

        private static Rect GetAddColumnButtonRect(Rect tableRect) =>
            new(tableRect.Right + 6, tableRect.Top, 24, Math.Max(24, tableRect.Height));

        private static Rect GetAddRowButtonRect(Rect tableRect) =>
            new(tableRect.Left, tableRect.Bottom + 6, Math.Max(24, tableRect.Width), 24);

        private static bool TryGetTableGeometry(
            TextView textView,
            MarkdownTableBlock table,
            double scrollX,
            double scrollY,
            out Rect tableRect,
            out Rect headerRect,
            out List<double> pipeXPositions)
        {
            tableRect = default;
            headerRect = default;
            pipeXPositions = new List<double>();

            var document = textView.Document;
            if (document == null || !textView.VisualLinesValid || table.StartLine < 1 || table.EndLine > document.LineCount)
                return false;

            // Ищем любую уже построенную видимую строку таблицы (без вызова GetOrConstructVisualLine!)
            VisualLine? refVisualLine = null;
            DocumentLine? refDocLine = null;

            foreach (var vl in textView.VisualLines)
            {
                int lineNum = vl.FirstDocumentLine.LineNumber;
                if (lineNum >= table.StartLine && lineNum <= table.EndLine && lineNum != table.StartLine + 1)
                {
                    refVisualLine = vl;
                    refDocLine = vl.FirstDocumentLine;
                    break;
                }
            }

            // Если ни одна строка таблицы не видна на экране — рисовать её не нужно
            if (refVisualLine == null || refDocLine == null)
                return false;

            pipeXPositions = GetPipeXPositions(document.GetText(refDocLine), refVisualLine, scrollX);
            if (pipeXPositions.Count < 2)
                return false;

            double left = pipeXPositions[0];
            double right = pipeXPositions[^1];
            double width = Math.Max(0, right - left);

            double lineHeight = refVisualLine.Height > 0 ? refVisualLine.Height : textView.DefaultLineHeight;
            double top = textView.GetVisualTopByDocumentLine(table.StartLine) - scrollY - 2;

            double headerBottom = table.StartLine + 2 <= table.EndLine
                ? textView.GetVisualTopByDocumentLine(table.StartLine + 2) - scrollY
                : top + lineHeight + 2;

            double bottom;
            if (table.EndLine < document.LineCount)
            {
             
                double totalHeight = 0;
                for (int ln = table.StartLine; ln <= table.EndLine; ln++)
                {
                    var docLn = document.GetLineByNumber(ln);
                    var vl = textView.GetVisualLine(ln);
                    totalHeight += vl?.Height ?? textView.DefaultLineHeight;
                }
                bottom = top + totalHeight + 4;
            }
            else
            {
                var lastVl = textView.GetVisualLine(table.EndLine);
                double lastHeight = lastVl?.Height ?? lineHeight;
                bottom = (textView.GetVisualTopByDocumentLine(table.EndLine) + lastHeight) - scrollY + 2;
            }

            tableRect = new Rect(left, top, width, Math.Max(0, bottom - top));
            headerRect = new Rect(left, top, width, Math.Max(0, headerBottom - top));
            return true;
        }

        private static List<double> GetPipeXPositions(string lineText, VisualLine visualLine, double scrollX)
        {
            var positions = new List<double>();

            for (int i = 0; i < lineText.Length; i++)
            {
                if (lineText[i] == '|' && (i == 0 || lineText[i - 1] != '\\'))
                {
                    int visualCol = visualLine.GetVisualColumn(i);
                    double x = visualLine.GetVisualPosition(visualCol, VisualYPosition.LineTop).X - scrollX;
                    positions.Add(x + 4);
                }
            }

            return positions;
        }
    }
}