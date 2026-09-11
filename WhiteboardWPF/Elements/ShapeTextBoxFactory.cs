namespace WhiteboardWPF.Elements
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using WhiteboardWPF.Models;

    /// <summary>
    /// Erzeugt die TextBox für ein Shape.
    /// Die Event-Handler werden weiterhin von MainWindow bereitgestellt.
    /// </summary>
    public sealed class ShapeTextBoxFactory
    {
        public TextBox Create(ShapeElement shape, MouseButtonEventHandler mouseDoubleClickHandler, KeyEventHandler keyDownHandler, RoutedEventHandler lostFocusHandler)
        {
            var textBox = new TextBox
            {
                Text = shape.Text,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8),
                FontSize = 16,
                IsReadOnly = true,
                IsHitTestVisible = true,
                Cursor = Cursors.Arrow,
                Tag = shape
            };

            textBox.MouseDoubleClick += mouseDoubleClickHandler;
            textBox.KeyDown += keyDownHandler;
            textBox.LostFocus += lostFocusHandler;

            return textBox;
        }
    }
}
