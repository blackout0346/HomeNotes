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
            foreach (var block in _state.Blocks)
            {
                if (block.StartOffset < startOffset) continue;
                if (_state.IsCaretInsideBlock(block)) continue;
                return block.StartOffset;
            }
            return -1;
        }
 
        public override VisualLineElement? ConstructElement(int offset)
        {
            foreach (var block in _state.Blocks)
            {
                if (block.StartOffset != offset) continue;
                if (_state.IsCaretInsideBlock(block)) continue;
 
                var control = block switch
                {
                    MarkdownImageBlock image => MarkdownImageElement.Build(
                        MarkdownImageLoader.Load(image.Url, VaultRootPath), image.AltText),
                    MarkdownTableBlock table => MarkdownTableElement.Build(table),
                    _ => null
                };
 
                if (control == null) continue;
                return new InlineObjectElement(block.DocumentLength, control);
            }
            return null;
        }
    }