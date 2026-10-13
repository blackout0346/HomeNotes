using System;
using Avalonia.Input;
using AvaloniaEdit;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Rendering;

namespace HomeNotes.Desktop.Markdown.Preview
{
    public class MarkdownPreviewManager : IDisposable
    {
        private MarkdownTableVisualElementGenerator? _tableGenerator;
        private MarkdownVisualElementGenerator? _generator;
        private MarkdownLiveColorizer? _colorizer;
        private CodeBlockBackgroundRenderer? _bgRenderer;
        private TextEditor? _editor;
        private bool _updatingTableCell;

        public string? VaultRootPath { get; set; }
        public MarkdownPreviewState State { get; } = new();

        public void Attach(TextEditor editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            Detach();
            _editor = editor;

            // Инициализация UI-генераторов
            _generator = new MarkdownVisualElementGenerator(State) { VaultRootPath = VaultRootPath };
            _tableGenerator = new MarkdownTableVisualElementGenerator(State, editor, UpdateTableCell);
            _editor.TextArea.TextView.ElementGenerators.Add(_generator);
            _editor.TextArea.TextView.ElementGenerators.Add(_tableGenerator);

            // Инициализация графических рендереров
            _colorizer = new MarkdownLiveColorizer(State);
            _bgRenderer = new CodeBlockBackgroundRenderer(State);
            _editor.TextArea.TextView.LineTransformers.Add(_colorizer);
            _editor.TextArea.TextView.BackgroundRenderers.Add(_bgRenderer);

            // Регистрация событий
            _editor.Document.TextChanged += OnTextChanged;
            _editor.TextArea.Caret.PositionChanged += OnCaretChanged;
            _editor.TextArea.PointerMoved += OnPointerMoved;
            _editor.TextArea.PointerExited += OnPointerExited;

            Reparse();
            UpdateCaretLine();
        }

        public void Detach()
        {
            if (_editor == null) return;

            _editor.Document.TextChanged -= OnTextChanged;
            _editor.TextArea.Caret.PositionChanged -= OnCaretChanged;
            _editor.TextArea.PointerMoved -= OnPointerMoved;
            _editor.TextArea.PointerExited -= OnPointerExited;

            if (_generator != null) _editor.TextArea.TextView.ElementGenerators.Remove(_generator);
            if (_tableGenerator != null)
            {
                _tableGenerator.ResetCollapsedLines();
                _editor.TextArea.TextView.ElementGenerators.Remove(_tableGenerator);
            }
            if (_colorizer != null) _editor.TextArea.TextView.LineTransformers.Remove(_colorizer);
            if (_bgRenderer != null) _editor.TextArea.TextView.BackgroundRenderers.Remove(_bgRenderer);

            _editor.TextArea.TextView.Redraw();
            _editor = null;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_editor == null || _colorizer == null) return;

            var point = e.GetPosition(_editor.TextArea.TextView);
            var visualLine = _editor.TextArea.TextView.GetVisualLineFromVisualTop(point.Y + _editor.TextArea.TextView.ScrollOffset.Y);
            int line = visualLine?.FirstDocumentLine?.LineNumber ?? -1;

            // Избегаем спама вызовами Redraw(): перерисовываем только если мышь перешла на другую строку
            if (_colorizer.HoveredLine != line)
            {
                _colorizer.HoveredLine = line;
                _editor.TextArea.TextView.Redraw();
            }
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            if (_colorizer != null && _colorizer.HoveredLine != -1)
            {
                _colorizer.HoveredLine = -1;
                _editor?.TextArea.TextView.Redraw();
            }
        }

        private void OnTextChanged(object? sender, EventArgs e)
        {
            if (_updatingTableCell) return;
            _tableGenerator?.ResetCollapsedLines();
            Reparse();
            _editor?.TextArea.TextView.Redraw();
        }

        private void OnCaretChanged(object? sender, EventArgs e)
        {
            UpdateCaretLine();
        }

        private void UpdateCaretLine()
        {
            if (_editor == null) return;

            int line = _editor.Document.GetLineByOffset(_editor.TextArea.Caret.Offset).LineNumber;
            if (line == State.CaretLine) return;

            State.CaretLine = line;
            if (_colorizer != null) _colorizer.CaretOffset = _editor.TextArea.Caret.Offset;

            _tableGenerator?.ResetCollapsedLines();
            _editor.TextArea.TextView.Redraw();
        }

        private void Reparse()
        {
            if (_editor == null) return;
            State.Blocks = MarkdownBlockParser.Parse(_editor.Document);
        }

        public void UpdateTableCell(MarkdownTableBlock table, int row, int column, string value)
        {
            if (_editor?.Document == null || _updatingTableCell) return;
            if (row < 0 || column < 0 || column >= table.Headers.Count || row > table.Rows.Count) return;

            int lineNumber = table.StartLine + (row == 0 ? 0 : row + 1);
            if (lineNumber < 1 || lineNumber > _editor.Document.LineCount) return;

            var line = _editor.Document.GetLineByNumber(lineNumber);
            var cells = _editor.Document.GetText(line).Trim().Trim('|').Split('|');

            if (column >= cells.Length) return;
            cells[column] = " " + value.Replace("|", "\\|") + " ";
            string updatedLine = "|" + string.Join("|", cells) + "|";

            _updatingTableCell = true;
            try
            {
                _tableGenerator?.ResetCollapsedLines();
                _editor.Document.Replace(line.Offset, line.Length, updatedLine);
            }
            finally
            {
                _updatingTableCell = false;
            }

            Reparse();
            _editor.TextArea.TextView.Redraw();
        }

        public void Dispose() => Detach();
    }
}