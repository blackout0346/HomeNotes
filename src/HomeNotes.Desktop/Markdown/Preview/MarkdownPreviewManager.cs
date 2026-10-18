using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using AvaloniaEdit;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Rendering;

namespace HomeNotes.Desktop.Markdown.Preview
{
    public class MarkdownPreviewManager : IDisposable
    {
        private MarkdownVisualElementGenerator? _generator;
        private MarkdownLiveColorizer? _colorizer;
        private CodeBlockBackgroundRenderer? _bgRenderer;
        private TableBackgroundRenderer? _tableBgRenderer;
        private TextEditor? _editor;
        private bool _isFormattingTable;
        private int _lastTypedOffset = -1;

        private Button? _copyButton;
        private Image? _copyIcon;
        private TextBlock? _copyText;
        private MarkdownCodeBlock? _activeCodeBlock;

        private static readonly IBrush CopiedBrush = new SolidColorBrush(Color.Parse("#50FA7B"));
        private static readonly IBrush DefaultTextBrush = new SolidColorBrush(Color.Parse("#A0A5B0"));
        private static readonly Cursor HandCursor = new(StandardCursorType.Hand);
        private static readonly Cursor IBeamCursor = new(StandardCursorType.Ibeam);

        public string? VaultRootPath { get; set; }
        public MarkdownPreviewState State { get; } = new();

        public void SetCopyButton(Button button, Image icon, TextBlock text)
        {
            _copyButton = button;
            _copyIcon = icon;
            _copyText = text;

            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var copySvg = new SvgImage { Source = SvgSource.Load($"avares://{assemblyName}/Assets/copy.svg", null) };
            var checkSvg = new SvgImage { Source = SvgSource.Load($"avares://{assemblyName}/Assets/check.svg", null) };

            _copyButton.Click += async (_, _) =>
            {
                if (_activeCodeBlock == null || _editor?.Document == null) return;

                int startLine = _activeCodeBlock.StartLine + 1;
                int endLine = _activeCodeBlock.EndLine - 1;

                if (startLine <= endLine)
                {
                    int startOffset = _editor.Document.GetLineByNumber(startLine).Offset;
                    int endOffset = _editor.Document.GetLineByNumber(endLine).EndOffset;
                    string code = _editor.Document.GetText(startOffset, endOffset - startOffset);

                    var topLevel = TopLevel.GetTopLevel(_editor);
                    if (topLevel?.Clipboard != null)
                    {
                        await topLevel.Clipboard.SetTextAsync(code);

                        if (_copyIcon != null) _copyIcon.Source = checkSvg;
                        if (_copyText != null)
                        {
                            _copyText.Text = "Скопировано!";
                            _copyText.Foreground = CopiedBrush;
                        }

                        DispatcherTimer.RunOnce(() =>
                        {
                            if (_copyIcon != null) _copyIcon.Source = copySvg;
                            if (_copyText != null)
                            {
                                _copyText.Text = "Копировать";
                                _copyText.Foreground = DefaultTextBrush;
                            }
                        }, TimeSpan.FromSeconds(2));
                    }
                }
            };
        }

        public void Attach(TextEditor editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            Detach();
            _editor = editor;

            _generator = new MarkdownVisualElementGenerator(State, editor) { VaultRootPath = VaultRootPath };
            _editor.TextArea.TextView.ElementGenerators.Add(_generator);

            _colorizer = new MarkdownLiveColorizer(State);
            _bgRenderer = new CodeBlockBackgroundRenderer(State);
            _tableBgRenderer = new TableBackgroundRenderer(State);

            _editor.TextArea.TextView.LineTransformers.Add(_colorizer);
            _editor.TextArea.TextView.BackgroundRenderers.Add(_bgRenderer);
            _editor.TextArea.TextView.BackgroundRenderers.Add(_tableBgRenderer);

            _editor.Document.TextChanged += OnTextChanged;
            _editor.TextArea.Caret.PositionChanged += OnCaretChanged;
            _editor.TextArea.PointerMoved += OnPointerMoved;
            _editor.TextArea.PointerExited += OnPointerExited;
            _editor.TextArea.TextView.ScrollOffsetChanged += OnScrollChanged;
            _editor.TextArea.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            _editor.TextArea.AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);

            ReparseAndFormatTables();
            UpdateCaretLine();
        }

        public void Detach()
        {
            if (_editor == null) return;

            _editor.Document.TextChanged -= OnTextChanged;
            _editor.TextArea.Caret.PositionChanged -= OnCaretChanged;
            _editor.TextArea.PointerMoved -= OnPointerMoved;
            _editor.TextArea.PointerExited -= OnPointerExited;
            _editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollChanged;
            _editor.TextArea.RemoveHandler(InputElement.KeyDownEvent, OnPreviewKeyDown);
            _editor.TextArea.RemoveHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed);

            if (_generator != null)
            {
                _generator.ResetCollapsedLines();
                _editor.TextArea.TextView.ElementGenerators.Remove(_generator);
            }
            if (_colorizer != null) _editor.TextArea.TextView.LineTransformers.Remove(_colorizer);
            if (_bgRenderer != null) _editor.TextArea.TextView.BackgroundRenderers.Remove(_bgRenderer);
            if (_tableBgRenderer != null) _editor.TextArea.TextView.BackgroundRenderers.Remove(_tableBgRenderer);

            if (_copyButton != null) _copyButton.IsVisible = false;
            _activeCodeBlock = null;
            State.HoveredTable = null;
            State.HoveredTableTarget = TableAddTarget.None;
            State.IsHoveringTableButton = false;

            _editor.TextArea.TextView.Redraw();
            _editor = null;
        }

        private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_editor?.Document == null || _tableBgRenderer == null)
                return;

            var point = e.GetPosition(_editor.TextArea.TextView);
            if (_tableBgRenderer.HitTestTableInteraction(
                    _editor.TextArea.TextView,
                    point,
                    out var hoveredTable,
                    out var target,
                    out bool isHoveringButton) &&
                hoveredTable != null &&
                isHoveringButton)
            {
                e.Handled = true;

                _isFormattingTable = true;
                try
                {
                    _generator?.ResetCollapsedLines();

                    if (target == TableAddTarget.Column)
                        MarkdownTableFormatter.AddColumn(_editor.Document, hoveredTable, _editor);
                    else if (target == TableAddTarget.Row)
                        MarkdownTableFormatter.AddRow(_editor.Document, hoveredTable, _editor);

                    State.Blocks = MarkdownBlockParser.Parse(_editor.Document);
                    var updatedTable = State.Blocks.OfType<MarkdownTableBlock>()
                        .FirstOrDefault(t => t.StartLine == hoveredTable.StartLine);

                    if (updatedTable != null)
                    {
                        MarkdownTableFormatter.FormatTable(_editor.Document, updatedTable, _editor);
                        State.Blocks = MarkdownBlockParser.Parse(_editor.Document);
                        State.HoveredTable = State.Blocks.OfType<MarkdownTableBlock>()
                            .FirstOrDefault(t => t.StartLine == updatedTable.StartLine);
                    }
                }
                finally
                {
                    _isFormattingTable = false;
                }

                _editor.TextArea.TextView.Redraw();
                _editor.TextArea.Focus();
            }
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (_editor?.Document == null) return;

            int line = _editor.Document.GetLineByOffset(_editor.TextArea.Caret.Offset).LineNumber;
            var table = State.Blocks.OfType<MarkdownTableBlock>().FirstOrDefault(t => t.ContainsLine(line));

            if (table != null)
            {
                MarkdownTableFormatter.HandleKeyDown(_editor, table, e);
            }
        }

        private void OnTextChanged(object? sender, EventArgs e)
        {
            if (_isFormattingTable || _editor?.Document == null) return;

            _generator?.ResetCollapsedLines();
            State.Blocks = MarkdownBlockParser.Parse(_editor.Document);
            _lastTypedOffset = _editor.TextArea.Caret.Offset;

            _editor.TextArea.TextView.Redraw();
        }

        private void ReparseAndFormatTables()
        {
            if (_editor?.Document == null) return;
            if (_generator != null) _generator.VaultRootPath = VaultRootPath;

            _isFormattingTable = true;
            try
            {
                _generator?.ResetCollapsedLines();
                State.Blocks = MarkdownBlockParser.Parse(_editor.Document);

                foreach (var table in State.Blocks.OfType<MarkdownTableBlock>())
                {
                    MarkdownTableFormatter.FormatTable(_editor.Document, table, _editor);
                }

                State.Blocks = MarkdownBlockParser.Parse(_editor.Document);
            }
            finally
            {
                _isFormattingTable = false;
            }
        }

        private void OnCaretChanged(object? sender, EventArgs e)
        {
            if (!_isFormattingTable)
                UpdateCaretLine();
        }

        private void UpdateCaretLine()
        {
            if (_editor?.Document == null) return;

            int caretOffset = _editor.TextArea.Caret.Offset;
            if (caretOffset != _lastTypedOffset)
                _lastTypedOffset = -1;

            var docLine = _editor.Document.GetLineByOffset(caretOffset);
            int line = docLine.LineNumber;

            var table = State.Blocks.OfType<MarkdownTableBlock>().FirstOrDefault(t => t.ContainsLine(line));
            if (table != null)
            {
                _isFormattingTable = true;
                try
                {
                    MarkdownTableFormatter.EnsureValidCaretPosition(_editor, table, _lastTypedOffset);
                }
                finally
                {
                    _isFormattingTable = false;
                }

                caretOffset = _editor.TextArea.Caret.Offset;
                line = _editor.Document.GetLineByOffset(caretOffset).LineNumber;
            }

            if (_colorizer != null) _colorizer.CaretOffset = caretOffset;
            if (line == State.CaretLine) return;

            State.CaretLine = line;
            _editor.TextArea.TextView.Redraw();
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_editor == null || _colorizer == null) return;

            var textView = _editor.TextArea.TextView;
            var point = e.GetPosition(textView);

            // 1. Проверяем наведение на таблицу и кнопки '+' справа/снизу
            if (_tableBgRenderer != null)
            {
                _tableBgRenderer.HitTestTableInteraction(
                    textView,
                    point,
                    out var hoveredTable,
                    out var target,
                    out bool isHoveringButton);

                if (!ReferenceEquals(State.HoveredTable, hoveredTable) ||
                    State.HoveredTableTarget != target ||
                    State.IsHoveringTableButton != isHoveringButton)
                {
                    State.HoveredTable = hoveredTable;
                    State.HoveredTableTarget = target;
                    State.IsHoveringTableButton = isHoveringButton;
                    _editor.TextArea.Cursor = isHoveringButton ? HandCursor : IBeamCursor;
                    textView.InvalidateVisual();
                }
            }

            // 2. Проверяем наведение на блок кода
            var visualLine = textView.GetVisualLineFromVisualTop(point.Y + textView.ScrollOffset.Y);
            int line = visualLine?.FirstDocumentLine?.LineNumber ?? -1;

            var codeBlock = State.Blocks.OfType<MarkdownCodeBlock>()
                .FirstOrDefault(b => line >= b.StartLine && line <= b.EndLine);

            bool blockChanged = _activeCodeBlock != codeBlock;
            _colorizer.HoveredLine = line;

            if (blockChanged)
            {
                _activeCodeBlock = codeBlock;
                textView.Redraw();
            }

            if (_copyButton == null) return;

            if (codeBlock != null)
            {
                if (blockChanged || !_copyButton.IsVisible)
                {
                    double top = textView.GetVisualTopByDocumentLine(codeBlock.StartLine) - textView.ScrollOffset.Y;
                    double left = textView.Bounds.Width - 110;

                    if (left > 0 && top >= 0)
                    {
                        Canvas.SetLeft(_copyButton, left);
                        Canvas.SetTop(_copyButton, top + 8);
                        _copyButton.IsVisible = true;
                    }
                }
            }
            else if (_copyButton.IsVisible)
            {
                DispatcherTimer.RunOnce(() =>
                {
                    if (_copyButton.IsPointerOver) return;
                    _copyButton.IsVisible = false;
                }, TimeSpan.FromMilliseconds(50));
            }
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            if (State.HoveredTable != null || State.HoveredTableTarget != TableAddTarget.None)
            {
                State.HoveredTable = null;
                State.HoveredTableTarget = TableAddTarget.None;
                State.IsHoveringTableButton = false;
                if (_editor != null)
                {
                    _editor.TextArea.Cursor = IBeamCursor;
                    _editor.TextArea.TextView.InvalidateVisual();
                }
            }

            if (_colorizer != null && _colorizer.HoveredLine != -1)
            {
                _colorizer.HoveredLine = -1;
                _activeCodeBlock = null;
                _editor?.TextArea.TextView.Redraw();
            }

            if (_copyButton != null && !_copyButton.IsPointerOver)
            {
                _copyButton.IsVisible = false;
            }
        }

        private void OnScrollChanged(object? sender, EventArgs e)
        {
            if (_copyButton != null) _copyButton.IsVisible = false;
        }

        public void Dispose() => Detach();
    }
}
