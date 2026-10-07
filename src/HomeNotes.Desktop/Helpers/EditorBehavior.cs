using System;
using Avalonia;
using AvaloniaEdit;
// Добавьте эти using, если их не было:
using HomeNotes.Desktop.Markdown.Rendering;
using HomeNotes.Desktop.Markdown.Preview;

namespace HomeNotes.Desktop.Helpers;

public class EditorBehavior
{
    public static readonly AttachedProperty<MarkdownLiveColorizer?> ColorizerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, MarkdownLiveColorizer?>("Colorizer");

    private static readonly AttachedProperty<EventHandler?> CaretHandlerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, EventHandler?>("CaretHandler");

    // НОВОЕ: Свойство для привязки PreviewManager
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
        {
            oldManager.Detach();
        }
        if (e.NewValue is MarkdownPreviewManager newManager)
        {
            newManager.Attach(editor);
        }
    }

    public static MarkdownLiveColorizer? GetColorizer(TextEditor element) => element.GetValue(ColorizerProperty);
    public static void SetColorizer(TextEditor element, MarkdownLiveColorizer? value) => element.SetValue(ColorizerProperty, value);

    private static void OnColorizerChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        var oldHandler = editor.GetValue(CaretHandlerProperty);
        if (oldHandler != null)
        {
            editor.TextArea.Caret.PositionChanged -= oldHandler;
        }

        // БЕЗОПАСНОЕ УДАЛЕНИЕ: Очищаем старые кастомные фоны
        var backgroundRenderers = editor.TextArea.TextView.BackgroundRenderers;
        for (int i = backgroundRenderers.Count - 1; i >= 0; i--)
        {
            if (backgroundRenderers[i] is CodeBlockBackgroundRenderer)
            {
                backgroundRenderers.RemoveAt(i);
            }
        }

        if (e.NewValue is MarkdownLiveColorizer colorizer)
        {
            // 1. Добавляем отрисовщик фона для блоков кода
            backgroundRenderers.Add(new CodeBlockBackgroundRenderer());

            // 2. БЕЗОПАСНОЕ УДАЛЕНИЕ: Очищаем старый колоризатор текста
            var transformers = editor.TextArea.TextView.LineTransformers;
            for (int i = transformers.Count - 1; i >= 0; i--)
            {
                if (transformers[i] is MarkdownLiveColorizer)
                {
                    transformers.RemoveAt(i);
                }
            }
            transformers.Add(colorizer);

            EventHandler newHandler = (sender, args) =>
            {
                colorizer.CaretOffset = editor.TextArea.Caret.Offset;
                editor.TextArea.TextView.Redraw();
            };

            editor.SetValue(CaretHandlerProperty, newHandler);
            editor.TextArea.Caret.PositionChanged += newHandler;
        }
    }
}