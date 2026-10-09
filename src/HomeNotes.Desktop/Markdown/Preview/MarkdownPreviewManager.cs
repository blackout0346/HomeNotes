using System;
using AvaloniaEdit;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Rendering;

namespace HomeNotes.Desktop.Markdown.Preview
{
    public class MarkdownPreviewManager : IDisposable
    {
        private MarkdownVisualElementGenerator? _generator;
        private TextEditor? _editor;
        private bool _updatingTableCell;

        public string? VaultRootPath { get; set; }

        public MarkdownPreviewState State { get; } = new();

        public void Attach(TextEditor editor)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            Detach();

            _editor = editor;

            _generator = new MarkdownVisualElementGenerator(State)
            {
                VaultRootPath = VaultRootPath
            };

            _editor.TextArea.TextView.ElementGenerators.Add(_generator);

            _editor.Document.TextChanged += OnTextChanged;
            _editor.TextArea.Caret.PositionChanged += OnCaretChanged;

            Reparse();
            UpdateCaretLine();
        }

        public void Detach()
        {
            if (_editor == null)
                return;

            _editor.Document.TextChanged -= OnTextChanged;
            _editor.TextArea.Caret.PositionChanged -= OnCaretChanged;

            if (_generator != null)
            {
                _editor.TextArea.TextView.ElementGenerators.Remove(_generator);
                _generator = null;
            }

            _editor.TextArea.TextView.Redraw();

            _editor = null;
        }

        private void OnTextChanged(object? sender, EventArgs e)
        {
            if (_updatingTableCell)
                return;

            Reparse();
            _editor?.TextArea.TextView.Redraw();
        }

        private void OnCaretChanged(object? sender, EventArgs e)
        {
            UpdateCaretLine();
        }

        private void UpdateCaretLine()
        {
            if (_editor == null)
                return;

            int line = _editor.Document
                .GetLineByOffset(_editor.TextArea.Caret.Offset)
                .LineNumber;

            if (line == State.CaretLine)
                return;

            State.CaretLine = line;
            _editor.TextArea.TextView.Redraw();
        }

        private void Reparse()
        {
            if (_editor == null)
                return;

            State.Blocks = MarkdownBlockParser.Parse(_editor.Document);

            System.Diagnostics.Debug.WriteLine(
                $"[MD] Blocks found: {State.Blocks.Count}");

            foreach (var block in State.Blocks)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MD] {block.GetType().Name}: " +
                    $"lines {block.StartLine}-{block.EndLine}");
            }
        }

        public void UpdateTableCell(
            MarkdownTableBlock table,
            int row,
            int column,
            string value)
        {
            if (_editor?.Document == null || _updatingTableCell)
                return;

            if (row < 0 || column < 0)
                return;

            if (column >= table.Headers.Count)
                return;

            if (row > table.Rows.Count)
                return;

            var document = _editor.Document;

            // row 0 — заголовок;
            // row 1 и далее — строки данных.
            // Строка-разделитель Markdown пропускается.
            int lineNumber = table.StartLine +
                (row == 0 ? 0 : row + 1);

            if (lineNumber < 1 || lineNumber > document.LineCount)
                return;

            var line = document.GetLineByNumber(lineNumber);
            string originalLine = document.GetText(line);

            var cells = originalLine
                .Trim()
                .Trim('|')
                .Split('|');

            if (column >= cells.Length)
                return;

            cells[column] = " " + value.Replace("|", "\\|") + " ";

            string updatedLine = "|" + string.Join("|", cells) + "|";

            _updatingTableCell = true;

            try
            {
                document.Replace(line.Offset, line.Length, updatedLine);
            }
            finally
            {
                _updatingTableCell = false;
            }

            Reparse();
            _editor.TextArea.TextView.Redraw();
        }

        public void Dispose()
        {
            Detach();
        }
    }
}