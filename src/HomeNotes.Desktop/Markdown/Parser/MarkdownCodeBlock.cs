namespace HomeNotes.Desktop.Markdown.Parser;

public class MarkdownCodeBlock : MarkdownBlock
{
    public string Language { get; init; } = string.Empty;
}