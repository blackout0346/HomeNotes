using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Preview;
using HomeNotes.Desktop.Markdown.Storage;

namespace HomeNotes.Desktop.Markdown.Rendering
{
    public class MarkdownVisualElementGenerator : VisualLineElementGenerator
    {
        private readonly MarkdownPreviewState _state;
        private readonly TextEditor _editor;
        private readonly Dictionary<int, CollapsedLineSection> _collapsedSeparators = new();

        public string? VaultRootPath { get; set; }

        public MarkdownVisualElementGenerator(MarkdownPreviewState state, TextEditor editor)
        {
            _state = state;
            _editor = editor;
        }

        public void ResetCollapsedLines()
        {
            foreach (var section in _collapsedSeparators.Values.ToArray())
            {
                section.Uncollapse();
            }
            _collapsedSeparators.Clear();
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            var document = CurrentContext.Document;
            if (document == null)
                return -1;

            int bestOffset = -1;

            foreach (var block in _state.Blocks)
            {
                if (block is MarkdownImageBlock)
                {
                    if (block.StartOffset < startOffset || block.StartOffset > document.TextLength)
                        continue;

                    if (_state.IsCaretInsideBlock(block))
                        continue;

                    if (bestOffset == -1 || block.StartOffset < bestOffset)
                        bestOffset = block.StartOffset;
                }
                else if (block is MarkdownTableBlock table)
                {
                    if (table.StartLine < 1 || table.StartLine + 1 > document.LineCount)
                        continue;

                    var headerLine = document.GetLineByNumber(table.StartLine);
                    if (headerLine.Length < 1)
                        continue;

                    int bridgeOffset = headerLine.EndOffset - 1;
                    if (bridgeOffset >= startOffset && (bestOffset == -1 || bridgeOffset < bestOffset))
                        bestOffset = bridgeOffset;
                }
            }

            return bestOffset;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            var document = CurrentContext.Document;
            if (document == null)
                return null;

            foreach (var block in _state.Blocks)
            {
                if (block is MarkdownTableBlock table &&
                    table.StartLine >= 1 &&
                    table.StartLine + 1 <= document.LineCount)
                {
                    var headerLine = document.GetLineByNumber(table.StartLine);
                    if (headerLine.Length >= 1 && offset == headerLine.EndOffset - 1)
                    {
                        var sepLine = document.GetLineByNumber(table.StartLine + 1);
                        int spanLength = 1 + headerLine.DelimiterLength + sepLine.Length;

                        if (!_collapsedSeparators.ContainsKey(sepLine.LineNumber))
                        {
                            var section = _editor.TextArea.TextView.CollapseLines(sepLine, sepLine);
                            _collapsedSeparators[sepLine.LineNumber] = section;
                        }

                        return new CollapsedSeparatorBridgeElement(spanLength);
                    }
                }

                if (block is MarkdownImageBlock imageBlock && imageBlock.StartOffset == offset)
                {
                    if (_state.IsCaretInsideBlock(imageBlock))
                        return null;

                    var bitmap = MarkdownImageLoader.Load(imageBlock.Url, VaultRootPath);
                    Control control = MarkdownImageElement.Build(bitmap, imageBlock.AltText);

                    var currentLine = document.GetLineByOffset(offset);
                    int maxAvailableInLine = currentLine.EndOffset - offset;
                    int desiredLength = Math.Min(imageBlock.DocumentLength, maxAvailableInLine);

                    if (desiredLength <= 0)
                        return null;

                    return new InlineObjectElement(desiredLength, control);
                }
            }

            return null;
        }

        /// <summary>
        /// Текстовый элемент (без UI-контрола), который скрывает последний '|' заголовка
        /// и поглощает свернутую строку-разделитель |---|---| без модификации VisualTree.
        /// </summary>
        private sealed class CollapsedSeparatorBridgeElement : VisualLineElement
        {
            public CollapsedSeparatorBridgeElement(int documentLength)
                : base(1, documentLength)
            {
            }

            public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
            {
                TextRunProperties.SetForegroundBrush(Brushes.Transparent);
                return new TextCharacters("|", TextRunProperties);
            }
        }
    }
}