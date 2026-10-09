using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Preview;

namespace HomeNotes.Desktop.Markdown.Rendering
{
    public sealed class MarkdownTableVisualElementGenerator
        : VisualLineElementGenerator
    {
        private readonly MarkdownPreviewState _state;
        private readonly TextEditor _editor;
        private readonly Action<MarkdownTableBlock, int, int, string>
            _onCellChanged;

        private readonly Dictionary<MarkdownTableBlock, CollapsedLineSection>
            _collapsedSections = new();

        public MarkdownTableVisualElementGenerator(
            MarkdownPreviewState state,
            TextEditor editor,
            Action<MarkdownTableBlock, int, int, string> onCellChanged)
        {
            _state = state;
            _editor = editor;
            _onCellChanged = onCellChanged;
        }

        public void ResetCollapsedLines()
        {
            foreach (var section in _collapsedSections.Values.ToArray())
                section.Uncollapse();

            _collapsedSections.Clear();
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            foreach (var table in _state.Blocks
                         .OfType<MarkdownTableBlock>()
                         .OrderBy(t => t.StartOffset))
            {
                if (table.StartOffset < startOffset)
                    continue;

                if (table.ContainsLine(_state.CaretLine))
                    continue;

                return table.StartOffset;
            }

            return -1;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            var table = _state.Blocks
                .OfType<MarkdownTableBlock>()
                .FirstOrDefault(t => t.StartOffset == offset);

            if (table == null)
                return null;

            if (table.ContainsLine(_state.CaretLine))
                return null;

            var document = CurrentContext.Document;

            if (document == null)
                return null;

            if (table.StartLine < 1 ||
                table.EndLine > document.LineCount ||
                table.EndLine < table.StartLine)
            {
                return null;
            }

            int documentLength = table.EndOffset - table.StartOffset;

            if (documentLength <= 0)
                return null;

            if (!_collapsedSections.ContainsKey(table) &&
                table.EndLine > table.StartLine)
            {
                DocumentLine firstHiddenLine =
                    document.GetLineByNumber(table.StartLine + 1);

                DocumentLine lastHiddenLine =
                    document.GetLineByNumber(table.EndLine);

                var section = _editor.TextArea.TextView.CollapseLines(
                    firstHiddenLine,
                    lastHiddenLine);

                _collapsedSections.Add(table, section);
            }

            Control control = MarkdownTableElement.Build(
                table,
                (row, column, value) =>
                    _onCellChanged(table, row, column, value));

            return new InlineObjectElement(documentLength, control);
        }
    }
}