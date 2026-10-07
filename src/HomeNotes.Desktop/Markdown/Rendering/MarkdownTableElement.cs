using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Enum;

namespace HomeNotes.Desktop.Markdown.Rendering
{
    public static class MarkdownTableElement
    {
        public static Control Build(MarkdownTableBlock tableBlock)
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            for (int i = 0; i < tableBlock.Headers.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            }

            int totalRows = 1 + tableBlock.Rows.Count;
            for (int i = 0; i < totalRows; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            // Цвет рамок как в Obsidian
            var borderBrush = new SolidColorBrush(Color.Parse("#3C3F46"));

            void AddCell(string text, int row, int col, bool isHeader, MarkdownColumnAlignment alignment)
            {
                var textBlock = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontWeight = isHeader ? FontWeight.Bold : FontWeight.Normal,
                    Margin = new Thickness(12, 6),
                    TextAlignment = alignment == MarkdownColumnAlignment.Center ? TextAlignment.Center :
                                    alignment == MarkdownColumnAlignment.Right ? TextAlignment.Right :
                                    TextAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var border = new Border
                {
                    BorderBrush = borderBrush,
                    // Чтобы рамки не дублировались, рисуем их только снизу и справа, 
                    // а для первой строки и первого столбца добавляем верхнюю/левую грань
                    BorderThickness = new Thickness(
                        col == 0 ? 1 : 0, 
                        row == 0 ? 1 : 0, 
                        1, 
                        1),
                    Child = textBlock
                };

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                grid.Children.Add(border);
            }

            for (int col = 0; col < tableBlock.Headers.Count; col++)
            {
                var alignment = col < tableBlock.Alignments.Count ? tableBlock.Alignments[col] : MarkdownColumnAlignment.Left;
                AddCell(tableBlock.Headers[col], 0, col, true, alignment);
            }

            for (int row = 0; row < tableBlock.Rows.Count; row++)
            {
                for (int col = 0; col < tableBlock.Headers.Count; col++)
                {
                    var cellText = col < tableBlock.Rows[row].Count ? tableBlock.Rows[row][col] : string.Empty;
                    var alignment = col < tableBlock.Alignments.Count ? tableBlock.Alignments[col] : MarkdownColumnAlignment.Left;
                    AddCell(cellText, row + 1, col, false, alignment);
                }
            }

            return grid;
        }
    }
}