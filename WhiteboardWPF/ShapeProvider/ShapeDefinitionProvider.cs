namespace WhiteboardWPF.ShapeProvider
{
    using WhiteboardWPF.Models;

    public static class ShapeDefinitionProvider
    {
        private static readonly List<ShapeDefinition> _definitions = new()
            {
                new ShapeDefinition(ShapeType.Rectangle, "Rechteck"),
                new ShapeDefinition(ShapeType.RoundedRectangle, "Abgerundetes Rechteck"),
                new ShapeDefinition(ShapeType.Ellipse, "Ellipse"),
                new ShapeDefinition(ShapeType.Diamond, "Raute"),
                new ShapeDefinition(ShapeType.Triangle,"Dreieck")
            };


        public static IReadOnlyList<ShapeDefinition> Definitions => _definitions;


        public static ShapeDefinition? GetDefinition(ShapeType type)
        {
            return _definitions.FirstOrDefault(definition => definition.Type == type);
        }
    }
}
