using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;

namespace HomeNotes.Desktop.Markdown.Rendering
{
    public class CodeBlockBackgroundRenderer : IBackgroundRenderer
    {
        private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#26282E"));
        private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#3C3F46")), 1);

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            var document = textView.Document;

            if (document == null)
                return;

            var blocks = MarkdownBlockParser.Parse(document);
            var codeBlocks = blocks.OfType<MarkdownCodeBlock>().ToList();

            double scrollY = textView.ScrollOffset.Y;
            double scrollX = textView.ScrollOffset.X;

            foreach (var block in codeBlocks)
            {
                // Координаты относительно НАЧАЛА ДОКУМЕНТА
                double documentTop =
                    textView.GetVisualTopByDocumentLine(block.StartLine);

                double documentBottom;

                if (block.EndLine < document.LineCount)
                {
                    documentBottom =
                        textView.GetVisualTopByDocumentLine(block.EndLine + 1);
                }
                else
                {
                    var lastVisualLine =
                        textView.GetOrConstructVisualLine(
                            document.GetLineByNumber(block.EndLine));

                    documentBottom =
                        lastVisualLine.VisualTop +
                        lastVisualLine.Height;
                }

                // Переводим координаты документа
                // в координаты текущего viewport
                double top = documentTop - scrollY;
                double bottom = documentBottom - scrollY;

                // Небольшой отступ
                top -= 2;
                bottom += 2;

                // Если блок полностью за экраном — не рисуем
                if (bottom < 0 || top > textView.Bounds.Height)
                    continue;

                double x = -10;
                double width = textView.Bounds.Width + 20;

                var rect = new Rect(
                    x,
                    top,
                    width,
                    Math.Max(0, bottom - top)
                );

                drawingContext.DrawRectangle(
                    BackgroundBrush,
                    BorderPen,
                    rect,
                    6,
                    6
                );
            }
        }
    }
}