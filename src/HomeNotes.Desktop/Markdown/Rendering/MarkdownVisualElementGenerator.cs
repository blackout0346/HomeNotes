using System;
using Avalonia.Controls;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Preview;
using HomeNotes.Desktop.Markdown.Storage;

namespace HomeNotes.Desktop.Markdown.Rendering;

// Заменяет текст картинок и таблиц на настоящие виджеты — но только на строках,
    // где сейчас НЕТ курсора. На активной строке остаётся исходный markdown-текст
    // (его раскрашивает MarkdownLiveColorizer), как в Obsidian: редактируете —
    // видите разметку, ушли курсором — видите результат.
    //
    // ВАЖНО: InlineObjectElement.DocumentLength может охватывать несколько строк
    // документа (включая переводы строк) — именно так заменяется многострочная
    // таблица одним виджетом, тем же механизмом, которым AvaloniaEdit сворачивает
    // многострочные блоки при folding.
    public class MarkdownVisualElementGenerator : VisualLineElementGenerator
    {
        private readonly MarkdownPreviewState _state;
        public string? VaultRootPath { get; set; }
 
        public MarkdownVisualElementGenerator(MarkdownPreviewState state)
        {
            _state = state;
        }
 
        public override int GetFirstInterestedOffset(int startOffset)
        {
            Console.WriteLine(
                $"[GENERATOR] GetFirstInterestedOffset start={startOffset}");
            int result = -1;

            foreach (var block in _state.Blocks)
            {
                if (block.StartOffset < startOffset)
                    continue;

                if (_state.IsCaretInsideBlock(block))
                    continue;

                if (result == -1 ||
                    block.StartOffset < result)
                {
                    result = block.StartOffset;
                }
            }
            Console.WriteLine($"[GENERATOR] result={result}");

            return result;
        }
 
        public override VisualLineElement? ConstructElement(int offset)
        {
            Console.WriteLine($"[GENERATOR] ConstructElement offset={offset}");
            foreach (var block in _state.Blocks)
            {
                Console.WriteLine(
                    $"[GENERATOR] block={block.GetType().Name}, " +
                    $"Start={block.StartOffset}, End={block.EndOffset}");
                if (block.StartOffset != offset)
                    continue;

                if (_state.IsCaretInsideBlock(block))
                    continue;
                Console.WriteLine("[GENERATOR] НАШЛИ BLOCK!");

                Control? control = null;

                var image = block as MarkdownImageBlock;

                if (image != null)
                {
                    control = MarkdownImageElement.Build(
                        MarkdownImageLoader.Load(
                            image.Url,
                            VaultRootPath),
                        image.AltText);
                }

                var table = block as MarkdownTableBlock;

                if (table != null)
                {
                    control = MarkdownTableElement.Build(table);
                }

                if (control == null)
                    continue;

                if (block.StartLine != block.EndLine)
                    continue;

                return new InlineObjectElement(
                    block.DocumentLength,
                    control);
            }

            return null;
        }
    }