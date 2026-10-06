namespace HomeNotes.Desktop.Markdown.Parser;

public class MarkdownImageBlock : MarkdownBlock
{
    public string AltText { get; init; } =  string.Empty;
    public string Url { get; init; } =  string.Empty;
}