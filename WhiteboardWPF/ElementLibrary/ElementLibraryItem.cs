namespace WhiteboardWPF.ElementLibrary
{
    using WhiteboardWPF.Models;

    public class ElementLibraryItem
    {
        public ElementLibraryItem(string name, string category, Func<object> createModel, ShapeType? shapeType = null)
        {
            Name = name;
            Category = category;
            ShapeType = shapeType;
            CreateModel = createModel;
        }

        public string Name { get; }
        public string Category { get; }
        public ShapeType? ShapeType { get; }
        public Func<object> CreateModel { get; }
    }
}
