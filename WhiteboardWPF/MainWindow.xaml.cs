namespace WhiteboardWPF
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    using WhiteboardWPF.Models;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _contextMenuPosition;

        private Border? _selectedShape;

        private bool _isDragging;

        private Point _dragStartMousePosition;

        private double _dragStartShapeX;
        private double _dragStartShapeY;


        public MainWindow()
        {
            InitializeComponent();

            StatusText.Text = "Whiteboard bereit";
        }


        // ============================================================
        // Whiteboard - rechte Maustaste
        // ============================================================

        private void WhiteBoardCanvas_MouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            /*
             * Position merken, an der später das neue Shape
             * eingefügt wird.
             */

            _contextMenuPosition = e.GetPosition(WhiteBoardCanvas);

            StatusText.Text = $"Position: X={_contextMenuPosition.X:0}, " + $"Y={_contextMenuPosition.Y:0}";
        }


        // ============================================================
        // Whiteboard - linke Maustaste
        // ============================================================

        private void WhiteBoardCanvas_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            /*
             * Klick auf das leere Whiteboard.
             *
             * Dadurch wird eine eventuell vorhandene Auswahl
             * aufgehoben.
             */

            SelectShape(null);
        }


        // ============================================================
        // Shape erstellen
        // ============================================================

        private void AddShape_Click(
            object sender,
            RoutedEventArgs e)
        {
            var shape = new ShapeElement
            {
                ShapeType = ShapeType.Rectangle,

                X = _contextMenuPosition.X,
                Y = _contextMenuPosition.Y,

                Width = 160,
                Height = 90
            };

            var control = CreateShapeControl(shape);

            WhiteBoardCanvas.Children.Add(control);

            /*
             * Das Shape wird direkt ausgewählt.
             */

            SelectShape(control);

            StatusText.Text =
                "Shape erstellt";

            /*
             * Contextmenü schließen.
             */

            WhiteBoardContextMenu.IsOpen = false;
        }


        // ============================================================
        // Shape-Control erzeugen
        // ============================================================

        private Border CreateShapeControl(
            ShapeElement shape)
        {
            var border = new Border
            {
                Width = shape.Width,
                Height = shape.Height,

                Background = Brushes.White,

                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(2),

                CornerRadius = new CornerRadius(4),

                Tag = shape
            };


            /*
             * Position auf dem Canvas.
             */

            Canvas.SetLeft(border, shape.X);
            Canvas.SetTop(border, shape.Y);


            /*
             * Mausereignisse.
             */

            border.MouseLeftButtonDown +=
                Shape_MouseLeftButtonDown;

            border.MouseMove +=
                Shape_MouseMove;

            border.MouseLeftButtonUp +=
                Shape_MouseLeftButtonUp;


            return border;
        }


        // ============================================================
        // Shape auswählen
        // ============================================================

        private void Shape_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Border shape)
                return;

            SelectShape(shape);


            /*
             * Dragging vorbereiten.
             */

            _isDragging = true;

            _dragStartMousePosition =
                e.GetPosition(WhiteBoardCanvas);


            _dragStartShapeX =
                Canvas.GetLeft(shape);

            _dragStartShapeY =
                Canvas.GetTop(shape);


            shape.CaptureMouse();

            e.Handled = true;
        }


        // ============================================================
        // Shape bewegen
        // ============================================================

        private void Shape_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            if (sender is not Border shape)
                return;


            Point currentMousePosition =
                e.GetPosition(WhiteBoardCanvas);


            /*
             * Mausbewegung seit Beginn des Dragging.
             */

            double deltaX =
                currentMousePosition.X -
                _dragStartMousePosition.X;

            double deltaY =
                currentMousePosition.Y -
                _dragStartMousePosition.Y;


            /*
             * Neue Position berechnen.
             */

            double newX =
                _dragStartShapeX + deltaX;

            double newY =
                _dragStartShapeY + deltaY;


            /*
             * Shape auf dem Canvas verschieben.
             */

            Canvas.SetLeft(shape, newX);
            Canvas.SetTop(shape, newY);


            /*
             * Position auch im Datenmodell aktualisieren.
             */

            if (shape.Tag is ShapeElement model)
            {
                model.X = newX;
                model.Y = newY;
            }


            StatusText.Text =
                $"Shape: X={newX:0}, Y={newY:0}";
        }


        // ============================================================
        // Dragging beenden
        // ============================================================

        private void Shape_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Border shape)
                return;


            _isDragging = false;

            shape.ReleaseMouseCapture();

            e.Handled = true;


            if (shape.Tag is ShapeElement model)
            {
                StatusText.Text =
                    $"Shape positioniert: " +
                    $"X={model.X:0}, Y={model.Y:0}";
            }
        }


        // ============================================================
        // Auswahl
        // ============================================================

        private void SelectShape(
            Border? shape)
        {
            /*
             * Alte Auswahl zurücksetzen.
             */

            if (_selectedShape != null)
            {
                _selectedShape.BorderBrush =
                    Brushes.DimGray;

                _selectedShape.BorderThickness =
                    new Thickness(2);
            }


            _selectedShape = shape;


            /*
             * Neue Auswahl darstellen.
             */

            if (_selectedShape != null)
            {
                _selectedShape.BorderBrush =
                    Brushes.DodgerBlue;

                _selectedShape.BorderThickness =
                    new Thickness(3);

                Panel.SetZIndex(
                    _selectedShape,
                    GetHighestZIndex() + 1);
            }
        }


        // ============================================================
        // Höchsten ZIndex ermitteln
        // ============================================================

        private int GetHighestZIndex()
        {
            int highest = 0;

            foreach (UIElement element
                     in WhiteBoardCanvas.Children)
            {
                int zIndex =
                    Panel.GetZIndex(element);

                if (zIndex > highest)
                    highest = zIndex;
            }

            return highest;
        }


        // ============================================================
        // Datei - Neues Board
        // ============================================================

        private void NewBoard_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (WhiteBoardCanvas.Children.Count > 0)
            {
                MessageBoxResult result =
                    MessageBox.Show(
                        "Das aktuelle Whiteboard wird geleert.\n\n" +
                        "Fortfahren?",
                        "Neues Whiteboard",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }


            ClearBoard();

            StatusText.Text =
                "Neues Whiteboard erstellt";
        }


        // ============================================================
        // Beenden
        // ============================================================

        private void Exit_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }


        // ============================================================
        // Löschen
        // ============================================================

        private void Delete_Click(
            object sender,
            RoutedEventArgs e)
        {
            /*
             * In diesem Schritt löschen wir das ausgewählte Shape.
             */

            if (_selectedShape == null)
            {
                StatusText.Text =
                    "Kein Shape ausgewählt";

                return;
            }


            WhiteBoardCanvas.Children.Remove(
                _selectedShape);


            _selectedShape = null;


            StatusText.Text =
                "Shape gelöscht";
        }


        // ============================================================
        // Board leeren
        // ============================================================

        private void ClearBoard_Click(
            object sender,
            RoutedEventArgs e)
        {
            ClearBoard();

            StatusText.Text =
                "Whiteboard geleert";
        }


        private void ResetBoard_Click(
            object sender,
            RoutedEventArgs e)
        {
            ClearBoard();

            StatusText.Text =
                "Whiteboard zurückgesetzt";
        }


        private void ClearBoard()
        {
            _isDragging = false;

            _selectedShape = null;

            WhiteBoardCanvas.Children.Clear();
        }


        // ============================================================
        // Contextmenü
        // ============================================================

        private void CloseContextMenu_Click(
            object sender,
            RoutedEventArgs e)
        {
            WhiteBoardContextMenu.IsOpen = false;
        }


        // ============================================================
        // Noch nicht implementierte Elemente
        // ============================================================

        private void AddText_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowNotImplemented(
                "Text",
                "Texte werden in einem späteren Schritt implementiert.");
        }


        private void AddArrow_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowNotImplemented(
                "Pfeil",
                "Pfeile werden in einem späteren Schritt implementiert.");
        }


        private void ShowNotImplemented(
            string elementName,
            string message)
        {
            MessageBox.Show(
                message,
                elementName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            StatusText.Text =
                $"{elementName}: Funktion folgt später";
        }
    }
}