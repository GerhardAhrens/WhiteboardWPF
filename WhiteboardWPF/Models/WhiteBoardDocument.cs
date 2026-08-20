namespace WhiteboardWPF.Models
{
    public class WhiteBoardDocument
    {
        public int Version { get; set; } = 1;

        public List<ShapeElement> Shapes { get; set; } = new();

        public List<ArrowElement> Arrows { get; set; } = new();

        public List<TextElement> TextElements { get; set; } = new();

        public List<SymbolElement> Symbols { get; set; } = new();
    }
}
