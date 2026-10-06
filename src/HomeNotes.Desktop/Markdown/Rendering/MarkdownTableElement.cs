using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using HomeNotes.Desktop.Markdown.Enum;
using HomeNotes.Desktop.Markdown.Parser;

namespace HomeNotes.Desktop.Markdown.Rendering;

 public static class MarkdownTableElement
    {
        public static Control Build(MarkdownTableBlock table)
        {
            var grid = new Grid { Margin = new Thickness(0, 6) };
 
            for (int c = 0; c < table.Headers.Count; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 80 });
 
            for (int r = 0; r <= table.Rows.Count; r++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
 
            for (int c = 0; c < table.Headers.Count; c++)
                grid.Children.Add(MakeCell(table.Headers[c], table.Alignments[c], true, 0, c));
 
            for (int r = 0; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];
                for (int c = 0; c < table.Headers.Count; c++)
                {
                    var text = c < row.Count ? row[c] : "";
                    var align = c < table.Alignments.Count ? table.Alignments[c] : MarkdownColumnAlignment.Left;
                    grid.Children.Add(MakeCell(text, align, false, r + 1, c));
                }
            }
 
            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#17181A")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Child = grid
            };
        }
 
        private static Control MakeCell(string text, MarkdownColumnAlignment align, bool isHeader, int row, int col)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontWeight = isHeader ? FontWeight.Bold : FontWeight.Normal,
                Margin = new Thickness(10, 4),
                TextAlignment = align switch
                {
                    MarkdownColumnAlignment.Center => TextAlignment.Center,
                    MarkdownColumnAlignment.Right => TextAlignment.Right,
                    _ => TextAlignment.Left
                }
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, col);
            return tb;
        }
    }