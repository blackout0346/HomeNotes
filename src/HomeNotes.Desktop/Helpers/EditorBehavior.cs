using System;
using Avalonia;
using AvaloniaEdit;
using Avalonia.Input;
using HomeNotes.Desktop.Markdown.Rendering;
using HomeNotes.Desktop.Markdown.Preview;

namespace HomeNotes.Desktop.Helpers;

public class EditorBehavior
{
    public static readonly AttachedProperty<MarkdownLiveColorizer?> ColorizerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, MarkdownLiveColorizer?>("Colorizer");

    private static readonly AttachedProperty<EventHandler?> CaretHandlerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, EventHandler?>("CaretHandler");

    
    private static readonly AttachedProperty<EventHandler<PointerEventArgs>?> PointerMovedHandlerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, EventHandler<PointerEventArgs>?>("PointerMovedHandler");

    private static readonly AttachedProperty<EventHandler<PointerEventArgs>?> PointerExitedHandlerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, EventHandler<PointerEventArgs>?>("PointerExitedHandler");

    public static readonly AttachedProperty<MarkdownPreviewManager?> PreviewManagerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, MarkdownPreviewManager?>("PreviewManager");

    static EditorBehavior()
    {
        ColorizerProperty.Changed.AddClassHandler<TextEditor>(OnColorizerChanged);
        PreviewManagerProperty.Changed.AddClassHandler<TextEditor>(OnPreviewManagerChanged);
    }

    public static MarkdownPreviewManager? GetPreviewManager(TextEditor element) => element.GetValue(PreviewManagerProperty);

    public static void SetPreviewManager(TextEditor element, MarkdownPreviewManager? value) => element.SetValue(PreviewManagerProperty, value);

    private static void OnPreviewManagerChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is MarkdownPreviewManager oldManager)
            oldManager.Detach();

        if (e.NewValue is MarkdownPreviewManager newManager)
            newManager.Attach(editor);
    }

    public static MarkdownLiveColorizer? GetColorizer(TextEditor element) => element.GetValue(ColorizerProperty);

    public static void SetColorizer(TextEditor element, MarkdownLiveColorizer? value) => element.SetValue(ColorizerProperty, value);

    private static void OnColorizerChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        
        if (editor.GetValue(CaretHandlerProperty) is EventHandler oldCaretHandler)
            editor.TextArea.Caret.PositionChanged -= oldCaretHandler;

        if (editor.GetValue(PointerMovedHandlerProperty) is EventHandler<PointerEventArgs> oldPointerMoved)
            editor.TextArea.PointerMoved -= oldPointerMoved;

        if (editor.GetValue(PointerExitedHandlerProperty) is EventHandler<PointerEventArgs> oldPointerExited)
            editor.TextArea.PointerExited -= oldPointerExited;

        editor.SetValue(CaretHandlerProperty, null);
        editor.SetValue(PointerMovedHandlerProperty, null);
        editor.SetValue(PointerExitedHandlerProperty, null);

        var transformers = editor.TextArea.TextView.LineTransformers;

        for (int i = transformers.Count - 1; i >= 0; i--)
        {
            if (transformers[i] is MarkdownLiveColorizer)
                transformers.RemoveAt(i);
        }

        RemoveCodeBlockRenderers(editor);

        if (e.NewValue is MarkdownLiveColorizer colorizer)
        {
            editor.TextArea.TextView.BackgroundRenderers.Add(new CodeBlockBackgroundRenderer());
            transformers.Add(colorizer);

            // Обработчик клавиатуры и курсора
            EventHandler newCaretHandler = (sender, args) =>
            {
                colorizer.CaretOffset = editor.TextArea.Caret.Offset;
                editor.TextArea.TextView.Redraw();
            };
            editor.SetValue(CaretHandlerProperty, newCaretHandler);
            editor.TextArea.Caret.PositionChanged += newCaretHandler;

            // Обработчик движения мыши (Hover)
            EventHandler<PointerEventArgs> newPointerMoved = (sender, args) =>
            {
                var point = args.GetPosition(editor.TextArea.TextView);
                var visualLine = editor.TextArea.TextView.GetVisualLineFromVisualTop(point.Y + editor.TextArea.TextView.ScrollOffset.Y);
                int line = visualLine?.FirstDocumentLine?.LineNumber ?? -1;

                if (colorizer.HoveredLine != line)
                {
                    colorizer.HoveredLine = line;
                    editor.TextArea.TextView.Redraw();
                }
            };
            editor.SetValue(PointerMovedHandlerProperty, newPointerMoved);
            editor.TextArea.PointerMoved += newPointerMoved;

            // Обработчик ухода мыши
            EventHandler<PointerEventArgs> newPointerExited = (sender, args) =>
            {
                if (colorizer.HoveredLine != -1)
                {
                    colorizer.HoveredLine = -1;
                    editor.TextArea.TextView.Redraw();
                }
            };
            editor.SetValue(PointerExitedHandlerProperty, newPointerExited);
            editor.TextArea.PointerExited += newPointerExited;
        }

        editor.TextArea.TextView.Redraw();
    }

    private static void RemoveCodeBlockRenderers(TextEditor editor)
    {
        var renderers = editor.TextArea.TextView.BackgroundRenderers;

        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            if (renderers[i] is CodeBlockBackgroundRenderer)
                renderers.RemoveAt(i);
        }
    }
}