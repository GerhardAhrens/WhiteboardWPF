namespace WhiteboardWPF.Persistence
{
    using WhiteboardWPF.Models;

    /// <summary>
    /// Erstellt ein WhiteBoardDocument aus bereits vorhandenen Modelldaten.
    /// Enthält keine WPF-UI-Logik und keine Dateioperationen.
    /// </summary>
    public sealed class BoardDocumentBuilder
    {
        public WhiteBoardDocument Build(
            IEnumerable<ShapeElement> shapes,
            IEnumerable<TextElement> textElements,
            IEnumerable<ArrowElement> arrows,
            IEnumerable<SymbolElement> symbols)
        {
            return new WhiteBoardDocument
            {
                Version = 1,
                Shapes = shapes.ToList(),
                TextElements = textElements.ToList(),
                Arrows = arrows.ToList(),
                Symbols = symbols.ToList()
            };
        }
    }
}
