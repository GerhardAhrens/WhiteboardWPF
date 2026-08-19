namespace WhiteboardWPF.Models
{
    public class SymbolElement
    {
        public Guid Id { get; set; } =  Guid.CreateVersion7();

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; } = 80;

        public double Height { get; set; } = 80;

        public string SymbolType { get; set; } = "Info";
    }
}
