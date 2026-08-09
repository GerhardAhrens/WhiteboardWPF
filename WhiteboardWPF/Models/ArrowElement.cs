namespace WhiteboardWPF.Models
{
    public class ArrowElement
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public Guid SourceId { get; set; }

        public Guid TargetId { get; set; }
    }
}
