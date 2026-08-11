namespace WhiteboardWPF.Models
{
    public class TextElement
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; } = 200;

        public double Height { get; set; } = 60;

        public string Text { get; set; } = string.Empty;

        public double FontSize { get; set; } = 16;
    }
}
