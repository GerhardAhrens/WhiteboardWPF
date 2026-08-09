namespace WhiteboardWPF.Models
{
    public class ShapeElement
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public ShapeType ShapeType { get; set; } = ShapeType.Rectangle;

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; } = 160;

        public double Height { get; set; } = 90;
    }
}
