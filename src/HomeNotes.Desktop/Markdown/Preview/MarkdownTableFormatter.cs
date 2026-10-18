using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using HomeNotes.Desktop.Markdown.Enum;
using HomeNotes.Desktop.Markdown.Parser;

namespace HomeNotes.Desktop.Markdown.Preview
{
    public static class MarkdownTableFormatter
    {
        private const int MinColumnWidth = 8;

        public static void FormatTable(TextDocument document, MarkdownTableBlock table, TextEditor editor)
        {
            int colCount = table.Headers.Count;
            if (colCount == 0 || table.StartLine < 1 || table.EndLine > document.LineCount)
                return;

            int caretOffset = editor.TextArea.Caret.Offset;
            int caretLine = document.GetLineByOffset(caretOffset).LineNumber;

            var rowLines = new List<int> { table.StartLine };
            for (int line = table.StartLine + 2; line <= table.EndLine; line++)
                rowLines.Add(line);

            var parsedCells = new List<string[]>();
            int activeRowIdx = -1;
            int activeColIdx = -1;
            int caretInActiveContent = 0;

            for (int r = 0; r < rowLines.Count; r++)
            {
                var docLine = document.GetLineByNumber(rowLines[r]);
                string lineText = document.GetText(docLine);
                var pipeIndices = GetPipeIndices(lineText);

                var rowValues = new string[colCount];

                for (int c = 0; c < colCount; c++)
                {
                    if (c + 1 < pipeIndices.Count)
                    {
                        int cellStartInLine = pipeIndices[c] + 1;
                        int cellEndInLine = pipeIndices[c + 1];
                        string rawCell = lineText.Substring(cellStartInLine, cellEndInLine - cellStartInLine);

                        int absCellStart = docLine.Offset + cellStartInLine;
                        int absCellEnd = docLine.Offset + cellEndInLine;

                        if (rowLines[r] == caretLine && caretOffset >= absCellStart && caretOffset <= absCellEnd)
                        {
                            activeRowIdx = r;
                            activeColIdx = c;

                            int caretInCell = caretOffset - absCellStart;
                            string trimmedStart = rawCell.TrimStart();
                            int leadingSpaces = rawCell.Length - trimmedStart.Length;

                            if (trimmedStart.Length == 0)
                            {
                                // Разрешаем одиночный пробел в начале, если пользователь только что его ввел
                                rowValues[c] = string.Empty;
                                caretInActiveContent = 0;
                            }
                            else
                            {
                                int lastNonSpace = rawCell.TrimEnd().Length;
                                // Сохраняем максимум 1 пробел после текста, если пользователь нажал Пробел при вводе
                                int maxAllowedEnd = Math.Min(rawCell.Length, lastNonSpace + 1);
                                int contentEnd = Math.Clamp(caretInCell, lastNonSpace, maxAllowedEnd);
                                int contentStart = Math.Min(leadingSpaces, contentEnd);

                                rowValues[c] = rawCell.Substring(contentStart, contentEnd - contentStart);
                                caretInActiveContent = Math.Clamp(caretInCell - contentStart, 0, rowValues[c].Length);
                            }
                        }
                        else
                        {
                            rowValues[c] = rawCell.Trim();
                        }
                    }
                    else
                    {
                        rowValues[c] = string.Empty;
                    }
                }

                parsedCells.Add(rowValues);
            }

            var colWidths = new int[colCount];
            for (int c = 0; c < colCount; c++)
            {
                int maxLen = MinColumnWidth;
                for (int r = 0; r < parsedCells.Count; r++)
                {
                    if (parsedCells[r][c].Length > maxLen)
                        maxLen = parsedCells[r][c].Length;
                }
                colWidths[c] = maxLen;
            }

            int activeCaretOffsetInLine = -1;

            // 1. Обновляем строки данных и шапки снизу вверх
            for (int r = rowLines.Count - 1; r >= 0; r--)
            {
                var docLine = document.GetLineByNumber(rowLines[r]);
                string oldText = document.GetText(docLine);

                var formattedCells = new string[colCount];
                int activeCellPipeOffsetInLine = 0;
                int activeCellLeftPad = 0;
                int currentLength = 0;

                for (int c = 0; c < colCount; c++)
                {
                    var alignment = c < table.Alignments.Count
                        ? table.Alignments[c]
                        : MarkdownColumnAlignment.Left;

                    string content = parsedCells[r][c];
                    string padded = PadCell(content, colWidths[c], alignment, out int leftPad);

                    if (r == activeRowIdx && c == activeColIdx)
                    {
                        activeCellPipeOffsetInLine = currentLength;
                        activeCellLeftPad = leftPad;
                    }

                    formattedCells[c] = " " + padded + " ";
                    currentLength += 1 + formattedCells[c].Length;
                }

                string newText = "|" + string.Join("|", formattedCells) + "|";

                if (oldText != newText)
                {
                    document.Replace(docLine.Offset, docLine.Length, newText);
                }

                if (r == activeRowIdx && activeColIdx >= 0)
                {
                    activeCaretOffsetInLine = activeCellPipeOffsetInLine + 2 + activeCellLeftPad + caretInActiveContent;
                }

                // Синхронизируем ширину строки-разделителя |---|---|
                if (r == 0 && table.StartLine + 1 <= document.LineCount)
                {
                    var sepDocLine = document.GetLineByNumber(table.StartLine + 1);
                    string oldSep = document.GetText(sepDocLine);
                    var sepCells = new string[colCount];

                    for (int c = 0; c < colCount; c++)
                    {
                        var alignment = c < table.Alignments.Count
                            ? table.Alignments[c]
                            : MarkdownColumnAlignment.Left;
                        sepCells[c] = " " + BuildSeparatorCell(colWidths[c], alignment) + " ";
                    }

                    string newSep = "|" + string.Join("|", sepCells) + "|";
                    if (oldSep != newSep)
                    {
                        document.Replace(sepDocLine.Offset, sepDocLine.Length, newSep);
                    }
                }
            }

            // 2. Вычисляем итоговое смещение каретки ПОСЛЕ обновления всех строк выше активной!
            if (activeRowIdx >= 0 && activeCaretOffsetInLine >= 0)
            {
                var updatedActiveLine = document.GetLineByNumber(rowLines[activeRowIdx]);
                int finalCaretOffset = updatedActiveLine.Offset + activeCaretOffsetInLine;
                editor.TextArea.Caret.Offset = Math.Clamp(finalCaretOffset, updatedActiveLine.Offset, updatedActiveLine.EndOffset);
            }
        }

        /// <summary>
        /// Не позволяет каретке при клике мышью становиться в пустые пробелы выравнивания ячейки.
        /// </summary>
        public static void EnsureValidCaretPosition(TextEditor editor, MarkdownTableBlock table, int lastTypedOffset)
        {
            var document = editor.Document;
            if (document == null || editor.SelectionLength > 0)
                return;

            int caretOffset = editor.TextArea.Caret.Offset;
            if (caretOffset == lastTypedOffset)
                return;

            var docLine = document.GetLineByOffset(caretOffset);
            int lineNum = docLine.LineNumber;

            if (lineNum == table.StartLine + 1)
            {
                int targetLine = table.StartLine + 2 <= document.LineCount ? table.StartLine + 2 : table.StartLine;
                FocusCell(editor, document.GetLineByNumber(targetLine), 0);
                return;
            }

            string lineText = document.GetText(docLine);
            var pipes = GetPipeIndices(lineText);
            if (pipes.Count < 2)
                return;

            int posInLine = caretOffset - docLine.Offset;
            int colIdx = GetCellIndex(posInLine, pipes);

            GetCellContentBounds(lineText, pipes, colIdx, out int contentStart, out int contentEnd);

            if (posInLine < contentStart)
            {
                editor.TextArea.Caret.Offset = docLine.Offset + contentStart;
            }
            else if (posInLine > contentEnd)
            {
                editor.TextArea.Caret.Offset = docLine.Offset + contentEnd;
            }
        }

        public static void AddRow(TextDocument document, MarkdownTableBlock table, TextEditor editor)
        {
            if (table.EndLine < 1 || table.EndLine > document.LineCount)
                return;

            var endDocLine = document.GetLineByNumber(table.EndLine);
            var emptyCells = table.Headers.Select(h => " " + new string(' ', Math.Max(MinColumnWidth, h.Length)) + " ");
            string newRow = Environment.NewLine + "|" + string.Join("|", emptyCells) + "|";

            document.Insert(endDocLine.EndOffset, newRow);

            if (table.EndLine + 1 <= document.LineCount)
            {
                var insertedLine = document.GetLineByNumber(table.EndLine + 1);
                FocusCell(editor, insertedLine, 0);
            }
        }

        public static void AddColumn(TextDocument document, MarkdownTableBlock table, TextEditor editor)
        {
            if (table.StartLine < 1 || table.EndLine > document.LineCount)
                return;

            string emptyCell = " " + new string(' ', MinColumnWidth) + " |";
            string sepCell = " " + new string('-', MinColumnWidth) + " |";

            for (int lineNum = table.EndLine; lineNum >= table.StartLine; lineNum--)
            {
                var docLine = document.GetLineByNumber(lineNum);
                string text = document.GetText(docLine).TrimEnd();

                if (!text.EndsWith('|'))
                    text += "|";

                string suffix = lineNum == table.StartLine + 1 ? sepCell : emptyCell;
                document.Replace(docLine.Offset, docLine.Length, text + suffix);
            }

            var headerLine = document.GetLineByNumber(table.StartLine);
            FocusCell(editor, headerLine, table.Headers.Count);
        }

        public static bool HandleKeyDown(TextEditor editor, MarkdownTableBlock table, KeyEventArgs e)
        {
            var document = editor.Document;
            if (document == null) return false;

            int caretOffset = editor.TextArea.Caret.Offset;
            var docLine = document.GetLineByOffset(caretOffset);
            string lineText = document.GetText(docLine);
            int posInLine = caretOffset - docLine.Offset;

            var pipeIndices = GetPipeIndices(lineText);
            if (pipeIndices.Count < 2) return false;

            int currentCol = GetCellIndex(posInLine, pipeIndices);
            GetCellContentBounds(lineText, pipeIndices, currentCol, out int contentStart, out int contentEnd);

            if (e.Key == Key.Back && editor.SelectionLength == 0)
            {
                if (posInLine <= contentStart)
                {
                    e.Handled = true;
                    return true;
                }
            }
            else if (e.Key == Key.Delete && editor.SelectionLength == 0)
            {
                if (posInLine >= contentEnd)
                {
                    e.Handled = true;
                    return true;
                }
            }
            else if (e.Key == Key.Right && e.KeyModifiers == KeyModifiers.None && editor.SelectionLength == 0)
            {
                if (posInLine >= contentEnd)
                {
                    e.Handled = true;
                    MoveToAdjacentCell(editor, document, table, docLine.LineNumber, currentCol, backwards: false, placeAtStart: true);
                    return true;
                }
            }
            else if (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.None && editor.SelectionLength == 0)
            {
                if (posInLine <= contentStart)
                {
                    e.Handled = true;
                    MoveToAdjacentCell(editor, document, table, docLine.LineNumber, currentCol, backwards: true, placeAtStart: false);
                    return true;
                }
            }
            else if (e.Key == Key.Tab)
            {
                e.Handled = true;
                bool backwards = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                MoveToAdjacentCell(editor, document, table, docLine.LineNumber, currentCol, backwards, placeAtStart: false);
                return true;
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                int nextLineNum = docLine.LineNumber == table.StartLine
                    ? table.StartLine + 2
                    : docLine.LineNumber + 1;

                if (nextLineNum <= table.EndLine && nextLineNum <= document.LineCount)
                {
                    FocusCell(editor, document.GetLineByNumber(nextLineNum), currentCol);
                }
                else
                {
                    AddRow(document, table, editor);
                }
                return true;
            }
            else if (e.Key == Key.Down && docLine.LineNumber == table.StartLine)
            {
                if (table.StartLine + 2 <= table.EndLine && table.StartLine + 2 <= document.LineCount)
                {
                    e.Handled = true;
                    FocusCell(editor, document.GetLineByNumber(table.StartLine + 2), currentCol);
                    return true;
                }
            }
            else if (e.Key == Key.Up && docLine.LineNumber == table.StartLine + 2)
            {
                e.Handled = true;
                FocusCell(editor, document.GetLineByNumber(table.StartLine), currentCol);
                return true;
            }

            return false;
        }

        private static void MoveToAdjacentCell(
            TextEditor editor,
            TextDocument document,
            MarkdownTableBlock table,
            int currentLine,
            int currentCol,
            bool backwards,
            bool placeAtStart)
        {
            int colCount = table.Headers.Count;
            int targetCol = backwards ? currentCol - 1 : currentCol + 1;
            int targetLine = currentLine;

            if (targetCol >= colCount)
            {
                targetCol = 0;
                targetLine = currentLine == table.StartLine ? table.StartLine + 2 : currentLine + 1;
            }
            else if (targetCol < 0)
            {
                targetCol = colCount - 1;
                targetLine = currentLine == table.StartLine + 2 ? table.StartLine : currentLine - 1;
            }

            if (targetLine >= table.StartLine && targetLine <= table.EndLine && targetLine != table.StartLine + 1)
            {
                FocusCell(editor, document.GetLineByNumber(targetLine), targetCol, placeAtStart);
            }
        }

        public static void FocusCell(TextEditor editor, DocumentLine line, int colIndex, bool placeAtStart = false)
        {
            string text = editor.Document.GetText(line);
            var pipes = GetPipeIndices(text);
            if (colIndex < 0 || colIndex + 1 >= pipes.Count) return;

            GetCellContentBounds(text, pipes, colIndex, out int contentStart, out int contentEnd);
            editor.TextArea.Caret.Offset = line.Offset + (placeAtStart ? contentStart : contentEnd);
        }

        private static void GetCellContentBounds(string lineText, List<int> pipes, int colIndex, out int contentStart, out int contentEnd)
        {
            int cellStart = pipes[colIndex] + 1;
            int cellEnd = pipes[colIndex + 1];
            string rawCell = lineText.Substring(cellStart, cellEnd - cellStart);

            string trimmedStart = rawCell.TrimStart();
            if (trimmedStart.Length == 0)
            {
                contentStart = cellStart + Math.Min(1, rawCell.Length);
                contentEnd = contentStart;
            }
            else
            {
                int leadingSpaces = rawCell.Length - trimmedStart.Length;
                int trailingEnd = rawCell.TrimEnd().Length;
                contentStart = cellStart + leadingSpaces;
                contentEnd = cellStart + trailingEnd;
            }
        }

        private static int GetCellIndex(int posInLine, List<int> pipeIndices)
        {
            for (int i = 0; i < pipeIndices.Count - 1; i++)
            {
                if (posInLine >= pipeIndices[i] && posInLine <= pipeIndices[i + 1])
                    return i;
            }
            return Math.Max(0, pipeIndices.Count - 2);
        }

        private static string BuildSeparatorCell(int width, MarkdownColumnAlignment alignment)
        {
            int dashCount = Math.Max(3, width);
            return alignment switch
            {
                MarkdownColumnAlignment.Center => ":" + new string('-', Math.Max(1, dashCount - 2)) + ":",
                MarkdownColumnAlignment.Right => new string('-', Math.Max(1, dashCount - 1)) + ":",
                _ => new string('-', dashCount)
            };
        }

        private static string PadCell(string content, int width, MarkdownColumnAlignment alignment, out int leftPad)
        {
           
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int maxLineLen = lines.Max(l => l.Length);
            int pad = Math.Max(0, width - maxLineLen);

            switch (alignment)
            {
                case MarkdownColumnAlignment.Right:
                    leftPad = pad;
                    return content.PadLeft(width);
                case MarkdownColumnAlignment.Center:
                    leftPad = pad / 2;
                    return new string(' ', leftPad) + content + new string(' ', pad - leftPad);
                default:
                    leftPad = 0;
                    return content.PadRight(width);
            }
        }

        public static List<int> GetPipeIndices(string text)
        {
            var indices = new List<int>();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '|' && (i == 0 || text[i - 1] != '\\'))
                    indices.Add(i);
            }
            return indices;
        }
    }
}