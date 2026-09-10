namespace WhiteboardWPF.Elements
{
    using System.Windows.Controls;

    /// <summary>
    /// Gemeinsame Grundfunktionen für die Erzeugung von Whiteboard-Controls.
    /// Elementtyp-spezifische Visualisierung und Eventverdrahtung bleiben
    /// außerhalb dieser Klasse.
    /// </summary>
    public sealed class ElementControlFactory
    {
        public Grid CreateBaseGrid(double width, double height, object tag, double x, double y)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            var grid = new Grid
            {
                Width = width,
                Height = height,
                Tag = tag
            };

            Canvas.SetLeft(grid, x);
            Canvas.SetTop(grid, y);

            return grid;
        }
    }
}
