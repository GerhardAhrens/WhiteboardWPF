namespace WhiteboardWPF.Elements
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;

    /// <summary>
    /// Erzeugt Resize-Thumbs für ein Element.
    /// Die eigentliche Resize-Logik bleibt in MainWindow.
    /// </summary>
    public sealed class ResizeThumbFactory
    {
        public void Add(Grid grid,
            HorizontalAlignment horizontalAlignment,
            VerticalAlignment verticalAlignment,
            ResizeDirection direction,
            DragStartedEventHandler dragStartedHandler,
            DragDeltaEventHandler dragDeltaHandler,
            DragCompletedEventHandler dragCompletedHandler,
            Cursor cursor)
        {
            var thumb = new Thumb
            {
                Width = 10,
                Height = 10,

                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment,

                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),

                Cursor = cursor,

                Tag = direction,

                Visibility = Visibility.Collapsed
            };

            thumb.DragStarted += dragStartedHandler;
            thumb.DragDelta += dragDeltaHandler;
            thumb.DragCompleted += dragCompletedHandler;

            grid.Children.Add(thumb);
        }
    }
}
