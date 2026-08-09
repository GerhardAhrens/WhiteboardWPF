namespace WhiteboardWPF.Models
{
    public class ShapeElement
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public ShapeType ShapeType { get; set; } = ShapeType.Rectangle;

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; } = 160;

        public double Height { get; set; } = 90;

        public string Text { get; set; } = string.Empty;
    }
}
