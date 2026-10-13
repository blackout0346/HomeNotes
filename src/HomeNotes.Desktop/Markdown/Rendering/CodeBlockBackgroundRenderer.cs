using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Preview;

namespace HomeNotes.Desktop.Markdown.Rendering
{
    public class CodeBlockBackgroundRenderer : IBackgroundRenderer
    {
        private readonly MarkdownPreviewState _state;
        private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#26282E"));
        private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#3C3F46")), 1);

        public KnownLayer Layer => KnownLayer.Background;

        public CodeBlockBackgroundRenderer(MarkdownPreviewState state)
        {
            _state = state;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView.Document == null) return;

            // КРИТИЧНОЕ ИЗМЕНЕНИЕ: Используем кэшированные блоки
            var codeBlocks = _state.Blocks.OfType<MarkdownCodeBlock>().ToList();
            double scrollY = textView.ScrollOffset.Y;
           
            foreach (var block in codeBlocks)
            {
                double documentTop = textView.GetVisualTopByDocumentLine(block.StartLine);
                double documentBottom;

                if (block.EndLine < textView.Document.LineCount)
                {
                    documentBottom = textView.GetVisualTopByDocumentLine(block.EndLine + 1);
                }
                else
                {
                    var lastVisualLine = textView.GetOrConstructVisualLine(textView.Document.GetLineByNumber(block.EndLine));
                    documentBottom = lastVisualLine.VisualTop + lastVisualLine.Height;
                }

                double top = documentTop - scrollY - 2;
                double bottom = documentBottom - scrollY + 2;

                if (bottom < 0 || top > textView.Bounds.Height)
                    continue;

                double x = -10;
                double width = textView.Bounds.Width + 20;

                var rect = new Rect(x, top, width, Math.Max(0, bottom - top));
                drawingContext.DrawRectangle(BackgroundBrush, BorderPen, rect, 6, 6);
            }
        }
    }
}