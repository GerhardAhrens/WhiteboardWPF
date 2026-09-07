namespace WhiteboardWPF.ElementLibrary
{
    public class ElementLibrary
    {
        private readonly List<ElementLibraryItem> _items = new();

        public IReadOnlyList<ElementLibraryItem> Items => _items;

        public void Register(ElementLibraryItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            _items.Add(item);
        }
    }
}
