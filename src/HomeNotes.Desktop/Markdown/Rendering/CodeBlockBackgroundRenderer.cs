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
            var document = textView.Document;
            if (document == null || !textView.VisualLinesValid)
                return;

            double scrollY = textView.ScrollOffset.Y;

            foreach (var block in _state.Blocks.OfType<MarkdownCodeBlock>())
            {
                if (block.StartLine < 1 || block.EndLine > document.LineCount)
                    continue;

                double documentTop = textView.GetVisualTopByDocumentLine(block.StartLine);
                double documentBottom;

                if (block.EndLine < document.LineCount)
                {
                    documentBottom = textView.GetVisualTopByDocumentLine(block.EndLine + 1);
                }
                else
                {
                    var lastVisualLine = textView.GetVisualLine(block.EndLine);
                    double lineHeight = lastVisualLine?.Height ?? textView.DefaultLineHeight;
                    documentBottom = textView.GetVisualTopByDocumentLine(block.EndLine) + lineHeight;
                }

                double top = documentTop - scrollY - 2;
                double bottom = documentBottom - scrollY + 2;

                if (bottom < 0 || top > textView.Bounds.Height)
                    continue;

                var rect = new Rect(-10, top, textView.Bounds.Width + 20, Math.Max(0, bottom - top));
                drawingContext.DrawRectangle(BackgroundBrush, BorderPen, rect, 6, 6);
            }
        }
    }
}