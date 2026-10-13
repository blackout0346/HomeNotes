using Avalonia;
using AvaloniaEdit;
using HomeNotes.Desktop.Markdown.Preview;

namespace HomeNotes.Desktop.Helpers;

public static class EditorBehavior
{
    public static readonly AttachedProperty<MarkdownPreviewManager?> PreviewManagerProperty =
        AvaloniaProperty.RegisterAttached<TextEditor, MarkdownPreviewManager?>(
            "PreviewManager", 
            typeof(EditorBehavior));

    static EditorBehavior()
    {
        PreviewManagerProperty.Changed.AddClassHandler<TextEditor>(OnPreviewManagerChanged);
    }

    public static MarkdownPreviewManager? GetPreviewManager(TextEditor element) => 
        element.GetValue(PreviewManagerProperty);

    public static void SetPreviewManager(TextEditor element, MarkdownPreviewManager? value) => 
        element.SetValue(PreviewManagerProperty, value);

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
}