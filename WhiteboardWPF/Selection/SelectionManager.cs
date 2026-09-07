namespace WhiteboardWPF.Selection
{
    using System.Windows.Controls;
    using System.Windows.Shapes;

    /// <summary>
    /// Verwaltet ausschließlich den aktuellen Auswahlzustand des Whiteboards.
    /// Die Klasse enthält keine WPF-Visualisierungslogik.
    /// </summary>
    public sealed class SelectionManager
    {
        public Grid? SelectedShape { get; set; }

        public List<Grid> SelectedShapes { get; } = new();

        public Grid? SelectedTextElement { get; set; }

        public List<Grid> SelectedTextElements { get; } = new();

        public Grid? SelectedSymbol { get; set; }

        public List<Grid> SelectedSymbols { get; } = new();

        public Path? SelectedArrow { get; set; }

        public List<Path> SelectedArrows { get; } = new();
    }
}
