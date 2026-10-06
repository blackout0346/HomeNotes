using System;
using AvaloniaEdit;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Rendering;

namespace HomeNotes.Desktop.Markdown.Preview;

public class MarkdownPreviewManager : IDisposable
{
    public string? VaultRootPath { get; set; }
    public MarkdownPreviewState State { get; } = new();
    private MarkdownVisualElementGenerator? _generator;
    private TextEditor? _editor;

    public void Attach(TextEditor editor)
    {
        Detach();
        _editor = editor;
        _generator = new MarkdownVisualElementGenerator(State) { VaultRootPath = VaultRootPath };
        _editor.TextArea.TextView.ElementGenerators.Add(_generator);
        Reparse();
        _editor.Document.TextChanged += OnTextChanged;
        _editor.TextArea.Caret.PositionChanged += OnCaretChanged;
        UpdateCaretLine();
    }

    public void Detach()
    {
        if (_editor == null) return;
        if (_generator != null)
        {
            _editor.TextArea.TextView.ElementGenerators.Remove(_generator);
            _generator = null;
        }

        _editor.Document.TextChanged -= OnTextChanged;
        _editor.TextArea.Caret.PositionChanged -= OnCaretChanged;
        _editor = null;
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        Reparse();
        _editor?.TextArea.TextView.Redraw();
    }

    private void OnCaretChanged(object? sender, EventArgs e) => UpdateCaretLine();

    private void UpdateCaretLine()
    {
        if (_editor == null) return;

        int line = _editor.Document.GetLineByOffset(_editor.TextArea.Caret.Offset).LineNumber;
        if (line == State.CaretLine) return;

        State.CaretLine = line;
        _editor.TextArea.TextView.Redraw();
    }

    private void Reparse()
    {
        if (_editor == null)
            return;

        State.Blocks = MarkdownBlockParser.Parse(_editor.Document);

        foreach (var block in State.Blocks)
        {
            Console.WriteLine(
                $"[PREVIEW] {block.GetType().Name}: " +
                $"Start={block.StartOffset}, End={block.EndOffset}, " +
                $"StartLine={block.StartLine}, " +
                $"EndLine={block.EndLine}");
        }
    }

    public void Refresh()
    {
        if (_editor == null)
            return;

        Reparse();
        UpdateCaretLine();
        _editor.TextArea.TextView.Redraw();
    }
    public void OnDocumentChanged()
    {
        if (_editor == null)
            return;

        _editor.Document.TextChanged -= OnTextChanged;

        _generator = null;

        Reparse();

        _editor.Document.TextChanged += OnTextChanged;

        UpdateCaretLine();
        _editor.TextArea.TextView.Redraw();
    }
    public void Dispose() => Detach();
}