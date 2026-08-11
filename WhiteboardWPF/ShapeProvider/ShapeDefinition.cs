namespace WhiteboardWPF.ShapeProvider
{
    using WhiteboardWPF.Models;

    public class ShapeDefinition
    {
        public ShapeType Type { get; }

        public string Name { get; }

        public ShapeDefinition(ShapeType type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}
