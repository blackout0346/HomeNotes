using System;
using Avalonia;

using AvaloniaEdit;

using HomeNotes.Desktop.Markdown.Rendering;
using HomeNotes.Desktop.Markdown.Preview;

namespace HomeNotes.Desktop.Helpers;

public class EditorBehavior
{
    public static readonly AttachedProperty<MarkdownLiveColorizer?> ColorizerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, MarkdownLiveColorizer?>("Colorizer");

    private static readonly AttachedProperty<EventHandler?> CaretHandlerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, EventHandler?>("CaretHandler");


    public static readonly AttachedProperty<MarkdownPreviewManager?> PreviewManagerProperty =
        AvaloniaProperty.RegisterAttached<EditorBehavior, TextEditor, MarkdownPreviewManager?>("PreviewManager");

  

    

    static EditorBehavior()
    {
        ColorizerProperty.Changed.AddClassHandler<TextEditor>(
            OnColorizerChanged);

        PreviewManagerProperty.Changed.AddClassHandler<TextEditor>(
            OnPreviewManagerChanged);

      
    }

    public static MarkdownPreviewManager? GetPreviewManager(TextEditor element) =>
        element.GetValue(PreviewManagerProperty);

    public static void SetPreviewManager(TextEditor element, MarkdownPreviewManager? value) =>
        element.SetValue(PreviewManagerProperty, value);

   
    private static void OnPreviewManagerChanged(
        TextEditor editor,
        AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is MarkdownPreviewManager oldManager)
            oldManager.Detach();

        if (e.NewValue is MarkdownPreviewManager newManager)
            newManager.Attach(editor);

    }

    public static MarkdownLiveColorizer? GetColorizer(TextEditor element) => element.GetValue(ColorizerProperty);

    public static void SetColorizer(TextEditor element, MarkdownLiveColorizer? value) =>
        element.SetValue(ColorizerProperty, value);

    private static void OnColorizerChanged(
        TextEditor editor,
        AvaloniaPropertyChangedEventArgs e)
    {
        var oldHandler = editor.GetValue(CaretHandlerProperty);

        if (oldHandler != null)
            editor.TextArea.Caret.PositionChanged -= oldHandler;

        editor.SetValue(CaretHandlerProperty, null);

        var transformers = editor.TextArea.TextView.LineTransformers;

        for (int i = transformers.Count - 1; i >= 0; i--)
        {
            if (transformers[i] is MarkdownLiveColorizer)
                transformers.RemoveAt(i);
        }

        RemoveCodeBlockRenderers(editor);

        if (e.NewValue is MarkdownLiveColorizer colorizer)
        {
            editor.TextArea.TextView.BackgroundRenderers.Add(
                new CodeBlockBackgroundRenderer());

            transformers.Add(colorizer);

            EventHandler newHandler = (sender, args) =>
            {
                colorizer.CaretOffset = editor.TextArea.Caret.Offset;
                editor.TextArea.TextView.Redraw();
            };

            editor.SetValue(CaretHandlerProperty, newHandler);
            editor.TextArea.Caret.PositionChanged += newHandler;
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