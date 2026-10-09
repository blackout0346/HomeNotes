using System;
using Avalonia.Controls;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Preview;
using HomeNotes.Desktop.Markdown.Storage;

namespace HomeNotes.Desktop.Markdown.Rendering
{
    public class MarkdownVisualElementGenerator : VisualLineElementGenerator
    {
        private readonly MarkdownPreviewState _state;
        public string? VaultRootPath { get; set; }

        public MarkdownVisualElementGenerator(MarkdownPreviewState state)
        {
            _state = state;
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            var document = CurrentContext.Document;
            if (document == null)
                return -1;

            foreach (var block in _state.Blocks)
            {
                if (block is not MarkdownImageBlock)
                    continue;

                if (block.StartOffset < startOffset ||
                    block.StartOffset > document.TextLength)
                    continue;

                if (_state.IsCaretInsideBlock(block))
                    continue;

                return block.StartOffset;
            }

            return -1;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            var document = CurrentContext.Document;
            if (document == null)
                return null;

            foreach (var block in _state.Blocks)
            {
                if (block is not MarkdownImageBlock imageBlock)
                    continue;

                if (imageBlock.StartOffset != offset)
                    continue;

                if (_state.IsCaretInsideBlock(imageBlock))
                    return null;

                var bitmap = MarkdownImageLoader.Load(
                    imageBlock.Url,
                    VaultRootPath);

                Control control = MarkdownImageElement.Build(
                    bitmap,
                    imageBlock.AltText);

                var currentLine = document.GetLineByOffset(offset);
                int maxAvailableInLine = currentLine.EndOffset - offset;
                int desiredLength = Math.Min(
                    imageBlock.DocumentLength,
                    maxAvailableInLine);

                if (desiredLength <= 0)
                    return null;

                return new InlineObjectElement(desiredLength, control);
            }

            return null;
        }
    }
}