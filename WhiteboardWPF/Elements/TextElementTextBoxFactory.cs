namespace WhiteboardWPF.Elements
{
    using System.Windows;
    using System.Windows.Controls;

    using System.Windows.Input;

    using WhiteboardWPF.Models;

    /// <summary>
    /// Erzeugt die TextBox für ein Text-Element.
    /// Die Event-Handler werden weiterhin von MainWindow bereitgestellt.
    /// </summary>
    public sealed class TextElementTextBoxFactory
    {
        public TextBox Create(TextElement text, KeyEventHandler keyDownHandler, RoutedEventHandler lostFocusHandler)
        {
            var textBox = new TextBox
            {
                Text = text.Text,
                FontSize = text.FontSize,
                AcceptsReturn = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4),
                IsReadOnly = true,
                IsHitTestVisible = true,
                Cursor = Cursors.Arrow,
                Tag = text
            };

            textBox.KeyDown += keyDownHandler;
            textBox.LostFocus += lostFocusHandler;

            return textBox;
        }
    }
}
