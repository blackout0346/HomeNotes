using Avalonia.Controls;
using AvaloniaEdit.Rendering;
using HomeNotes.Desktop.Markdown.Parser;
using HomeNotes.Desktop.Markdown.Preview;
using HomeNotes.Desktop.Markdown.Storage;

namespace HomeNotes.Desktop.Markdown.Rendering
{
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
                if (block.StartOffset >= startOffset)
                {
                    if (_state.IsCaretInsideBlock(block)) continue;

                    if (block is MarkdownTableBlock || block is MarkdownImageBlock)
                    {
                        return block.StartOffset;
                    }
                }
            }
            return -1;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            foreach (var block in _state.Blocks)
            {
                if (block.StartOffset == offset)
                {
                    if (_state.IsCaretInsideBlock(block)) return null;

                    Control? control = null;

                    if (block is MarkdownTableBlock tableBlock)
                    {
                        control = MarkdownTableElement.Build(tableBlock);
                    }
                    else if (block is MarkdownImageBlock imageBlock)
                    {
                        var bitmap = MarkdownImageLoader.Load(imageBlock.Url, VaultRootPath);
                        control = MarkdownImageElement.Build(bitmap, imageBlock.AltText);
                    }

                    if (control != null)
                    {
                        var document = CurrentContext.Document;
                        var startLine = document.GetLineByNumber(block.StartLine);
                        
                      
                        int lengthToReplace = block is MarkdownTableBlock 
                            ? startLine.EndOffset - offset 
                            : block.DocumentLength;

                        return new InlineObjectElement(lengthToReplace, control);
                    }
                }
            }
            return null;
        }
    }
}