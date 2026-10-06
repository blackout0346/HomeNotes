using System;
using Avalonia;
using AvaloniaEdit;
using HomeNotes.Desktop.Markdown.Rendering;
using HomeNotes.Desktop.Services;

namespace HomeNotes.Desktop.Helpers;

public class EditorBehavior
{
    public static readonly AttachedProperty<MarkdownLiveColorizer?> ColorizerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, MarkdownLiveColorizer?>("Colorizer");

    private static readonly AttachedProperty<EventHandler?> CaretHandlerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, EventHandler?>("CaretHandler");

    static EditorBehavior()
    {
        ColorizerProperty.Changed.AddClassHandler<TextEditor>(OnColorizerChanged);
    }

    public static MarkdownLiveColorizer? GetColorizer(TextEditor element) => element.GetValue(ColorizerProperty);

    public static void SetColorizer(TextEditor element, MarkdownLiveColorizer? value) =>
        element.SetValue(ColorizerProperty, value);

    private static void OnColorizerChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        var oldHandler = editor.GetValue(CaretHandlerProperty);
        if (oldHandler != null)
        {
            editor.TextArea.Caret.PositionChanged -= oldHandler;
        }

        if (e.NewValue is MarkdownLiveColorizer colorizer)
        {
            // БЕЗОПАСНОЕ УДАЛЕНИЕ: Удаляем только наш колоризатор, оставляя системные (например, выделение текста)
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