using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace HomeNotes.Desktop.Markdown.Rendering;

public static class MarkdownImageElement
{
    public static Control Build(Bitmap? bitmap, string altText)
    {
        if (bitmap == null)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2A2D31")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4),
                Child = new TextBlock
                {
                    Text = string.IsNullOrEmpty(altText) ? "[изображение не найдено]" : $"[{altText}]",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyle.Italic
                }
            };
        }
 
        return new Image
        {
            Source = bitmap,
            MaxHeight = 320,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4)
        };
    }
}