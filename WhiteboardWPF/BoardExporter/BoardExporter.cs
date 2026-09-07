namespace WhiteboardWPF.Exporter
{
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using WhiteboardWPF.Selection;

    /// <summary>
    /// Exportiert das aktuelle WPF-Whiteboard als PNG.
    /// Die bestehende Auswahl und die Sichtbarkeit der Resize-Handles
    /// werden während des Exports temporär ausgeblendet und anschließend
    /// wiederhergestellt.
    /// </summary>
    public sealed class BoardExporter
    {
        public void ExportAsPng(
            string fileName,
            Canvas canvas,
            SelectionManager selection,
            Action<Grid, bool> setShapeSelectedVisual,
            Action<Grid, bool> setSymbolSelectedVisual,
            Action<System.Windows.Shapes.Path, bool> setArrowSelectedVisual,
            Action<Grid, Visibility> setResizeHandlesVisibility,
            Action updateLayout)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Ein Dateiname muss angegeben werden.", nameof(fileName));
            }

            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (setShapeSelectedVisual == null)
            {
                throw new ArgumentNullException(nameof(setShapeSelectedVisual));
            }

            if (setSymbolSelectedVisual == null)
            {
                throw new ArgumentNullException(nameof(setSymbolSelectedVisual));
            }

            if (setArrowSelectedVisual == null)
            {
                throw new ArgumentNullException(nameof(setArrowSelectedVisual));
            }

            if (setResizeHandlesVisibility == null)
                throw new ArgumentNullException(nameof(setResizeHandlesVisibility));

            if (updateLayout == null)
            {
                throw new ArgumentNullException(nameof(updateLayout));
            }

            updateLayout();

            var resizeThumbs = canvas.Children
                .OfType<Grid>()
                .SelectMany(grid => grid.Children.OfType<Thumb>())
                .ToList();

            var oldResizeVisibility = resizeThumbs
                .Select(thumb => thumb.Visibility)
                .ToList();

            try
            {
                foreach (Grid shape in selection.SelectedShapes.ToList())
                {
                    setShapeSelectedVisual(shape, false);
                    setResizeHandlesVisibility(shape, Visibility.Collapsed);
                }

                foreach (Grid text in selection.SelectedTextElements.ToList())
                {
                    setResizeHandlesVisibility(text, Visibility.Collapsed);
                }

                foreach (Grid symbol in selection.SelectedSymbols.ToList())
                {
                    setSymbolSelectedVisual(symbol, false);
                    setResizeHandlesVisibility(symbol, Visibility.Collapsed);
                }

                foreach (System.Windows.Shapes.Path arrow in selection.SelectedArrows.ToList())
                {
                    setArrowSelectedVisual(arrow, false);
                }

                updateLayout();

                int width = (int)Math.Ceiling(canvas.Width);
                int height = (int)Math.Ceiling(canvas.Height);

                if (width <= 0 || height <= 0)
                {
                    return;
                }

                var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                renderBitmap.Render(canvas);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using FileStream stream = File.Create(fileName);
                encoder.Save(stream);
            }
            finally
            {
                for (int i = 0; i < resizeThumbs.Count; i++)
                {
                    resizeThumbs[i].Visibility = oldResizeVisibility[i];
                }

                foreach (Grid shape in selection.SelectedShapes.ToList())
                {
                    setShapeSelectedVisual(shape, true);
                }

                foreach (Grid symbol in selection.SelectedSymbols.ToList())
                {
                    setSymbolSelectedVisual(symbol, true);
                }

                foreach (System.Windows.Shapes.Path arrow in selection.SelectedArrows.ToList())
                {
                    setArrowSelectedVisual(arrow, false);
                }

                updateLayout();
            }
        }
    }
}
