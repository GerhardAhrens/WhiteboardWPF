namespace WhiteboardWPF.Board
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Ermittelt und aktualisiert die Größe des Whiteboard-Canvas
    /// anhand der enthaltenen Elemente.
    /// </summary>
    public sealed class BoardSizeManager
    {
        private const double MinimumWidth = 800;
        private const double MinimumHeight = 500;
        private const double Margin = 50;

        /// <summary>
        /// Aktualisiert die Canvas-Größe.
        /// Die Berechnung entspricht der bisherigen UpdateBoardSize()-Logik.
        /// </summary>
        public void Update(Canvas canvas)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            double maxRight = 0;
            double maxBottom = 0;

            foreach (FrameworkElement element in canvas.Children.OfType<FrameworkElement>())
            {
                double left = Canvas.GetLeft(element);
                double top = Canvas.GetTop(element);

                if (double.IsNaN(left))
                {
                    left = 0;
                }

                if (double.IsNaN(top))
                {
                    top = 0;
                }

                double width = element.Width;
                double height = element.Height;

                if (double.IsNaN(width))
                {
                    width = element.ActualWidth;
                }

                if (double.IsNaN(height))
                {
                    height = element.ActualHeight;
                }

                maxRight = Math.Max(maxRight, left + width);
                maxBottom = Math.Max(maxBottom, top + height);
            }

            canvas.Width = Math.Max(MinimumWidth, maxRight + Margin);
            canvas.Height = Math.Max(MinimumHeight, maxBottom + Margin);
        }
    }
}
