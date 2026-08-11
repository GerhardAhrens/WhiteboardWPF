namespace WhiteboardWPF
{
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    using Microsoft.Win32;

    using WhiteboardWPF.Models;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _contextMenuPosition;

        private Grid? _selectedShape;

        private bool _isDragging;

        private Point _dragStartMousePosition;

        private double _dragStartShapeX;
        private double _dragStartShapeY;


        // ============================================================
        // Resize
        // ============================================================

        private const double MinimumShapeWidth = 40;

        private const double MinimumShapeHeight = 30;

        private bool _isResizing;

        private ResizeDirection _resizeDirection;

        private Point _resizeStartMousePosition;

        private double _resizeStartX;
        private double _resizeStartY;

        private double _resizeStartWidth;
        private double _resizeStartHeight;


        // ============================================================
        // Textbearbeitung
        // ============================================================

        private TextBox? _editingTextBox;

        private string _textBeforeEditing = string.Empty;


        // ============================================================
        // Pfeile
        // ============================================================
        private readonly List<ArrowElement> _arrows = new();

        private bool _isCreatingArrow;

        private Grid? _arrowSourceShape;
        
        private System.Windows.Shapes.Path? _selectedArrow;


        public MainWindow()
        {
            InitializeComponent();

            StatusText.Text = "Whiteboard bereit";
        }


        // ============================================================
        // Whiteboard - rechte Maustaste
        // ============================================================

        private void WhiteBoardCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _contextMenuPosition = e.GetPosition(WhiteBoardCanvas);

            StatusText.Text = $"Position: X={_contextMenuPosition.X:0}, Y={_contextMenuPosition.Y:0}";
        }


        // ============================================================
        // Whiteboard - linke Maustaste
        // ============================================================

        private void WhiteBoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectShape(null);
        }


        // ============================================================
        // Shape erstellen
        // ============================================================

        private void AddShape_Click(object sender, RoutedEventArgs e)
        {
            var shape = new ShapeElement
            {
                ShapeType = ShapeType.Rectangle,

                X = _contextMenuPosition.X,
                Y = _contextMenuPosition.Y,

                Width = 160,
                Height = 90,

                Text = string.Empty
            };

            var control = CreateShapeControl(shape);

            WhiteBoardCanvas.Children.Add(control);

            SelectShape(control);

            StatusText.Text = "Shape erstellt";

            WhiteBoardContextMenu.IsOpen = false;
        }


        // ============================================================
        // Shape-Control erzeugen
        // ============================================================

        private Grid CreateShapeControl(ShapeElement shape)
        {
            var grid = new Grid
            {
                Width = shape.Width,
                Height = shape.Height,

                Tag = shape
            };


            // --------------------------------------------------------
            // Shape
            // --------------------------------------------------------

            var shapeVisual =
                CreateShapeVisual(shape);

            grid.Children.Add(shapeVisual);


            // --------------------------------------------------------
            // Text
            // --------------------------------------------------------

            var textBox = new TextBox
            {
                Text = shape.Text,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8),
                FontSize = 16,
                IsReadOnly = true,
                IsHitTestVisible = true,
                Cursor = Cursors.Arrow,
                Tag = shape
            };

            textBox.MouseDoubleClick += ShapeText_MouseDoubleClick;
            textBox.KeyDown += ShapeTextBox_KeyDown;
            textBox.LostFocus += ShapeTextBox_LostFocus;

            grid.Children.Add(textBox);


            // --------------------------------------------------------
            // Contextmenü für das Shape
            // --------------------------------------------------------

            var shapeContextMenu = CreateShapeContextMenu();
            grid.ContextMenu = shapeContextMenu;

            // --------------------------------------------------------
            // Position
            // --------------------------------------------------------

            Canvas.SetLeft(grid, shape.X);
            Canvas.SetTop(grid, shape.Y);

            // --------------------------------------------------------
            // Verschieben
            // --------------------------------------------------------

            grid.PreviewMouseLeftButtonDown += Shape_PreviewMouseLeftButtonDown;
            grid.PreviewMouseMove += Shape_PreviewMouseMove;
            grid.PreviewMouseLeftButtonUp += Shape_PreviewMouseLeftButtonUp;

            // --------------------------------------------------------
            // Resize-Griffe
            // --------------------------------------------------------

            AddResizeThumb(grid, HorizontalAlignment.Left, VerticalAlignment.Top, ResizeDirection.TopLeft);
            AddResizeThumb(grid, HorizontalAlignment.Center, VerticalAlignment.Top, ResizeDirection.Top);
            AddResizeThumb(grid, HorizontalAlignment.Right, VerticalAlignment.Top, ResizeDirection.TopRight);
            AddResizeThumb(grid, HorizontalAlignment.Left,VerticalAlignment.Center, ResizeDirection.Left);
            AddResizeThumb(grid, HorizontalAlignment.Right, VerticalAlignment.Center, ResizeDirection.Right);
            AddResizeThumb(grid, HorizontalAlignment.Left, VerticalAlignment.Bottom, ResizeDirection.BottomLeft);
            AddResizeThumb(grid, HorizontalAlignment.Center, VerticalAlignment.Bottom, ResizeDirection.Bottom);
            AddResizeThumb(grid, HorizontalAlignment.Right, VerticalAlignment.Bottom, ResizeDirection.BottomRight);

            return grid;
        }


        // ============================================================
        // Contextmenü für Shape
        // ============================================================

        private ContextMenu CreateShapeContextMenu()
        {
            var contextMenu = new ContextMenu();


            var editTextItem = new MenuItem
            {
                Header = "Text bearbeiten"
            };

            editTextItem.Click += ShapeEditText_Click;

            contextMenu.Items.Add(editTextItem);


            return contextMenu;
        }


        // ============================================================
        // Text über Contextmenü bearbeiten
        // ============================================================

        private void ShapeEditText_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            if (menuItem.Parent is not ContextMenu contextMenu)
                return;

            if (contextMenu.PlacementTarget is not Grid shape)
                return;


            SelectShape(shape);

            BeginTextEditing(shape);
        }


        // ============================================================
        // Text über Doppelklick bearbeiten
        // ============================================================

        private void ShapeText_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            if (textBox.Parent is not Grid shape)
                return;


            SelectShape(shape);

            BeginTextEditing(shape);

            e.Handled = true;
        }


        // ============================================================
        // Textbearbeitung starten
        // ============================================================

        private void BeginTextEditing(
            Grid shape)
        {
            if (shape.Tag is not ShapeElement model)
                return;


            var textBox =
                FindTextBox(shape);

            if (textBox == null)
                return;


            _editingTextBox = textBox;

            _textBeforeEditing =
                model.Text;


            textBox.IsReadOnly = false;

            textBox.Focus();

            textBox.SelectAll();


            StatusText.Text =
                "Text bearbeiten";
        }


        // ============================================================
        // TextBox finden
        // ============================================================

        private TextBox? FindTextBox(
            Grid shape)
        {
            foreach (UIElement child
                     in shape.Children)
            {
                if (child is TextBox textBox)
                    return textBox;
            }

            return null;
        }


        // ============================================================
        // Tastatur in TextBox
        // ============================================================

        private void ShapeTextBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;


            // --------------------------------------------------------
            // Enter = übernehmen
            // --------------------------------------------------------

            if (e.Key == Key.Enter)
            {
                FinishTextEditing(textBox);

                e.Handled = true;

                return;
            }


            // --------------------------------------------------------
            // Escape = abbrechen
            // --------------------------------------------------------

            if (e.Key == Key.Escape)
            {
                CancelTextEditing(textBox);

                e.Handled = true;
            }
        }


        // ============================================================
        // Textbearbeitung beendet
        // ============================================================

        private void ShapeTextBox_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;


            if (_editingTextBox != textBox)
                return;


            FinishTextEditing(textBox);
        }


        // ============================================================
        // Text übernehmen
        // ============================================================

        private void FinishTextEditing(
            TextBox textBox)
        {
            if (textBox.Parent is not Grid shape)
                return;


            if (shape.Tag is not ShapeElement model)
                return;


            model.Text =
                textBox.Text;


            textBox.IsReadOnly = true;


            _editingTextBox = null;


            StatusText.Text =
                "Text übernommen";
        }


        // ============================================================
        // Textbearbeitung abbrechen
        // ============================================================

        private void CancelTextEditing(
            TextBox textBox)
        {
            if (textBox.Parent is not Grid shape)
                return;


            if (shape.Tag is not ShapeElement model)
                return;


            textBox.Text =
                _textBeforeEditing;

            model.Text =
                _textBeforeEditing;


            textBox.IsReadOnly = true;


            _editingTextBox = null;


            StatusText.Text =
                "Textbearbeitung abgebrochen";
        }


        // ============================================================
        // Resize Thumb hinzufügen
        // ============================================================

        private void AddResizeThumb(
            Grid grid,
            HorizontalAlignment horizontalAlignment,
            VerticalAlignment verticalAlignment,
            ResizeDirection direction)
        {
            var thumb = new Thumb
            {
                Width = 10,
                Height = 10,

                HorizontalAlignment =
                    horizontalAlignment,

                VerticalAlignment =
                    verticalAlignment,

                Background = Brushes.White,

                BorderBrush = Brushes.DodgerBlue,

                BorderThickness =
                    new Thickness(1),

                Cursor =
                    GetResizeCursor(direction),

                Tag = direction,

                Visibility =
                    Visibility.Collapsed
            };


            thumb.DragStarted +=
                ResizeThumb_DragStarted;

            thumb.DragDelta +=
                ResizeThumb_DragDelta;

            thumb.DragCompleted +=
                ResizeThumb_DragCompleted;


            grid.Children.Add(thumb);
        }


        // ============================================================
        // Resize Cursor
        // ============================================================

        private Cursor GetResizeCursor(
            ResizeDirection direction)
        {
            return direction switch
            {
                ResizeDirection.TopLeft =>
                    Cursors.SizeNWSE,

                ResizeDirection.Top =>
                    Cursors.SizeNS,

                ResizeDirection.TopRight =>
                    Cursors.SizeNESW,

                ResizeDirection.Left =>
                    Cursors.SizeWE,

                ResizeDirection.Right =>
                    Cursors.SizeWE,

                ResizeDirection.BottomLeft =>
                    Cursors.SizeNESW,

                ResizeDirection.Bottom =>
                    Cursors.SizeNS,

                ResizeDirection.BottomRight =>
                    Cursors.SizeNWSE,

                _ =>
                    Cursors.Arrow
            };
        }


        // ============================================================
        // Shape auswählen
        // ============================================================

        private void Shape_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Grid shape)
                return;


            // ========================================================
            // Pfeil erstellen
            // ========================================================

            if (_isCreatingArrow)
            {
                CreateArrow(
                    _arrowSourceShape,
                    shape);

                e.Handled = true;

                return;
            }


            // ========================================================
            // Resize-Griffe
            // ========================================================

            if (e.OriginalSource is Thumb)
                return;


            // ========================================================
            // Doppelklick auf Text
            // ========================================================

            if (e.ClickCount >= 2 &&
                e.OriginalSource is TextBox)
            {
                return;
            }


            // ========================================================
            // Normales Verschieben
            // ========================================================

            if (_editingTextBox != null)
                return;


            SelectShape(shape);


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

        private void CreateArrow(
    Grid? sourceShape,
    Grid targetShape)
        {
            if (sourceShape == null)
            {
                CancelArrowCreation();
                return;
            }


            if (sourceShape == targetShape)
            {
                StatusText.Text =
                    "Quelle und Ziel müssen unterschiedlich sein.";

                return;
            }


            if (sourceShape.Tag is not ShapeElement sourceModel)
            {
                CancelArrowCreation();
                return;
            }


            if (targetShape.Tag is not ShapeElement targetModel)
            {
                CancelArrowCreation();
                return;
            }


            var arrow = new ArrowElement
            {
                SourceId = sourceModel.Id,

                TargetId = targetModel.Id
            };


            _arrows.Add(arrow);


            DrawArrow(arrow);


            _isCreatingArrow = false;

            _arrowSourceShape = null;


            SelectShape(targetShape);


            StatusText.Text =
                "Pfeil erstellt";
        }

        private void DrawArrow(
            ArrowElement arrow)
        {
            Grid? sourceShape =
                FindShape(arrow.SourceId);

            Grid? targetShape =
                FindShape(arrow.TargetId);


            if (sourceShape == null ||
                targetShape == null)
            {
                return;
            }


            Point start =
                GetConnectionPoint(
                    sourceShape,
                    targetShape);


            Point end =
                GetConnectionPoint(
                    targetShape,
                    sourceShape);


            var path =
                new System.Windows.Shapes.Path
                {
                    Stroke =
                        Brushes.DimGray,

                    StrokeThickness =
                        2,

                    Fill =
                        Brushes.DimGray,

                    IsHitTestVisible =
                        true,

                    Tag =
                        arrow
                };


            CreateArrowGeometry(
                path,
                start,
                end);


            path.MouseLeftButtonDown +=
                Arrow_MouseLeftButtonDown;


            path.ContextMenu =
                CreateArrowContextMenu();


            path.ContextMenuOpening +=
                Arrow_ContextMenuOpening;


            Panel.SetZIndex(
                path,
                -1000);


            WhiteBoardCanvas.Children.Add(
                path);
        }

        private void Arrow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not System.Windows.Shapes.Path arrow)
                return;


            SelectArrow(arrow);
        }

        private Point GetConnectionPoint(
            Grid source,
            Grid target)
        {
            if (source.Tag is not ShapeElement sourceModel)
                return new Point();


            if (target.Tag is not ShapeElement targetModel)
                return new Point();


            Point sourceCenter =
                new Point(
                    sourceModel.X +
                        sourceModel.Width / 2,

                    sourceModel.Y +
                        sourceModel.Height / 2);


            Point targetCenter =
                new Point(
                    targetModel.X +
                        targetModel.Width / 2,

                    targetModel.Y +
                        targetModel.Height / 2);


            Vector direction =
                targetCenter -
                sourceCenter;


            if (direction.Length < 0.001)
                return sourceCenter;


            direction.Normalize();


            return sourceModel.ShapeType switch
            {
                ShapeType.Rectangle =>
                    GetRectangleConnectionPoint(
                        sourceModel,
                        sourceCenter,
                        direction),

                ShapeType.RoundedRectangle =>
                    GetRectangleConnectionPoint(
                        sourceModel,
                        sourceCenter,
                        direction),

                ShapeType.Ellipse =>
                    GetEllipseConnectionPoint(
                        sourceModel,
                        sourceCenter,
                        direction),

                ShapeType.Diamond =>
                    GetDiamondConnectionPoint(
                        sourceModel,
                        sourceCenter,
                        direction),

                _ =>
                    sourceCenter
            };
        }

        private Point GetRectangleConnectionPoint(
            ShapeElement shape,
            Point center,
            Vector direction)
        {
            double halfWidth =
                shape.Width / 2;

            double halfHeight =
                shape.Height / 2;


            double scaleX =
                Math.Abs(direction.X) < 0.000001
                    ? double.PositiveInfinity
                    : halfWidth /
                      Math.Abs(direction.X);


            double scaleY =
                Math.Abs(direction.Y) < 0.000001
                    ? double.PositiveInfinity
                    : halfHeight /
                      Math.Abs(direction.Y);


            double scale =
                Math.Min(
                    scaleX,
                    scaleY);


            return center +
                   direction * scale;
        }

        private Point GetEllipseConnectionPoint(
            ShapeElement shape,
            Point center,
            Vector direction)
        {
            double radiusX =
                shape.Width / 2;

            double radiusY =
                shape.Height / 2;


            double denominator =
                Math.Sqrt(
                    Math.Pow(
                        direction.X / radiusX,
                        2) +

                    Math.Pow(
                        direction.Y / radiusY,
                        2));


            if (denominator < 0.000001)
                return center;


            double scale =
                1 / denominator;


            return center +
                   direction * scale;
        }

        private Point GetDiamondConnectionPoint(
            ShapeElement shape,
            Point center,
            Vector direction)
        {
            double halfWidth =
                shape.Width / 2;

            double halfHeight =
                shape.Height / 2;


            double denominator =
                Math.Abs(direction.X) /
                    halfWidth

                +

                Math.Abs(direction.Y) /
                    halfHeight;


            if (denominator < 0.000001)
                return center;


            double scale =
                1 / denominator;


            return center +
                   direction * scale;
        }

        /*
        private Point GetRoundedRectangleConnectionPoint(Grid shape, Point center, Vector direction)
        {
            return GetRectangleConnectionPoint(
                shape,
                center,
                direction);
        }
        */

        private Grid? FindShape(Guid id)
        {
            foreach (UIElement element in WhiteBoardCanvas.Children)
            {
                if (element is Grid grid && grid.Tag is ShapeElement model && model.Id == id)
                {
                    return grid;
                }
            }

            return null;
        }

        private void CancelArrowCreation()
        {
            _isCreatingArrow = false;

            _arrowSourceShape = null;

            StatusText.Text = "Pfeilerstellung abgebrochen";
        }

        private void UpdateArrows()
        {
            foreach (ArrowElement arrow in _arrows.ToList())
            {
                UpdateArrow(arrow);
            }
        }

        private void UpdateArrow(ArrowElement arrow)
        {
            var path =
                WhiteBoardCanvas.Children
                    .OfType<System.Windows.Shapes.Path>()
                    .FirstOrDefault(p =>
                        ReferenceEquals(
                            p.Tag,
                            arrow));


            if (path == null)
                return;


            UpdateArrowPath(path, arrow);
        }

        private void UpdateArrowPath(
            System.Windows.Shapes.Path path,
            ArrowElement arrow)
        {
            Grid? sourceShape =
                FindShape(arrow.SourceId);

            Grid? targetShape =
                FindShape(arrow.TargetId);


            if (sourceShape == null ||
                targetShape == null)
            {
                return;
            }


            Point start =
                GetConnectionPoint(
                    sourceShape,
                    targetShape);


            Point end =
                GetConnectionPoint(
                    targetShape,
                    sourceShape);


            CreateArrowGeometry(
                path,
                start,
                end);
        }

        private void CreateArrowGeometry(System.Windows.Shapes.Path path, Point start, Point end)
        {
            Vector direction =
                end - start;


            if (direction.Length < 0.001)
                return;


            direction.Normalize();


            Vector perpendicular =
                new Vector(
                    -direction.Y,
                    direction.X);


            const double arrowLength = 12;

            const double arrowWidth = 6;


            Point arrowBase =
                end -
                direction * arrowLength;


            Point left =
                arrowBase +
                perpendicular * arrowWidth;


            Point right =
                arrowBase -
                perpendicular * arrowWidth;


            var geometry =
                new StreamGeometry();


            using (StreamGeometryContext context =
                   geometry.Open())
            {
                // ----------------------------------------------------
                // Linie
                // ----------------------------------------------------

                context.BeginFigure(
                    start,
                    false,
                    false);

                context.LineTo(
                    arrowBase,
                    true,
                    false);


                // ----------------------------------------------------
                // Pfeilspitze
                // ----------------------------------------------------

                context.BeginFigure(
                    left,
                    true,
                    true);

                context.LineTo(
                    end,
                    true,
                    false);

                context.LineTo(
                    right,
                    true,
                    false);
            }


            geometry.Freeze();


            path.Data =
                geometry;
        }

        private ContextMenu CreateArrowContextMenu()
        {
            var contextMenu = new ContextMenu();


            var deleteItem = new MenuItem
            {
                Header = "Pfeil löschen"
            };


            deleteItem.Click += ArrowDelete_Click;


            contextMenu.Items.Add(deleteItem);


            return contextMenu;
        }

        private void ArrowDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;


            if (menuItem.Parent is not ContextMenu contextMenu)
                return;


            if (contextMenu.PlacementTarget is not System.Windows.Shapes.Path path)
            {
                return;
            }


            if (path.Tag is not ArrowElement arrow)
                return;


            _arrows.Remove(arrow);


            WhiteBoardCanvas.Children.Remove(
                path);


            if (_selectedArrow == path)
            {
                _selectedArrow = null;
            }


            StatusText.Text = "Pfeil gelöscht";
        }

        private void SelectArrow(System.Windows.Shapes.Path? arrow)
        {
            // --------------------------------------------------------
            // Alte Pfeilauswahl entfernen
            // --------------------------------------------------------

            if (_selectedArrow != null)
            {
                SetArrowSelectedVisual(_selectedArrow, false);
            }


            // --------------------------------------------------------
            // Neue Pfeilauswahl
            // --------------------------------------------------------

            _selectedArrow = arrow;


            if (_selectedArrow != null)
            {
                SetArrowSelectedVisual(_selectedArrow, true);
            }
        }

        private void SetArrowSelectedVisual(System.Windows.Shapes.Path arrow, bool selected)
        {
            arrow.Stroke = selected ? Brushes.DodgerBlue : Brushes.DimGray;

            arrow.Fill = selected ? Brushes.DodgerBlue : Brushes.DimGray;

            arrow.StrokeThickness = selected ? 3 : 2;
        }

        private void Arrow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Shapes.Path arrow)
                return;


            SelectArrow(arrow);


            StatusText.Text = "Pfeil ausgewählt";


            e.Handled = true;
        }


        // ============================================================
        // Shape bewegen
        // ============================================================

        private void Shape_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            if (sender is not Grid shape)
                return;


            Point currentMousePosition = e.GetPosition(WhiteBoardCanvas);


            double deltaX = currentMousePosition.X -
                _dragStartMousePosition.X;

            double deltaY = currentMousePosition.Y -
                _dragStartMousePosition.Y;


            double newX = _dragStartShapeX + deltaX;

            double newY = _dragStartShapeY + deltaY;


            /*
             * Shape bewegen.
             */
            Canvas.SetLeft(shape, newX);

            Canvas.SetTop(shape, newY);


            /*
             * Datenmodell aktualisieren.
             */
            if (shape.Tag is ShapeElement model)
            {
                model.X = newX;
                model.Y = newY;
            }

            UpdateArrows();

            StatusText.Text = $"Shape: X={newX:0}, Y={newY:0}";


            e.Handled = true;
        }


        // ============================================================
        // Dragging beenden
        // ============================================================

        private void Shape_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Grid shape)
                return;


            if (!_isDragging)
                return;


            _isDragging = false;


            if (shape.IsMouseCaptured)
            {
                shape.ReleaseMouseCapture();
            }


            if (shape.Tag is ShapeElement model)
            {
                StatusText.Text = $"Shape positioniert: X={model.X:0}, Y={model.Y:0}";
            }


            e.Handled = true;
        }

        // ============================================================
        // Resize gestartet
        // ============================================================

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (sender is not Thumb thumb)
                return;

            if (thumb.Parent is not Grid shape)
                return;

            if (thumb.Tag is not ResizeDirection direction)
                return;


            SelectShape(shape);

            _isResizing = true;

            _resizeDirection = direction;

            _resizeStartMousePosition = Mouse.GetPosition(WhiteBoardCanvas);

            _resizeStartX = Canvas.GetLeft(shape);

            _resizeStartY = Canvas.GetTop(shape);

            _resizeStartWidth = shape.ActualWidth;

            _resizeStartHeight = shape.ActualHeight;


            StatusText.Text = "Größe ändern";
        }


        // ============================================================
        // Resize
        // ============================================================

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!_isResizing)
                return;

            if (sender is not Thumb thumb)
                return;

            if (thumb.Parent is not Grid shape)
                return;


            Point currentMousePosition = Mouse.GetPosition(WhiteBoardCanvas);


            double deltaX = currentMousePosition.X - _resizeStartMousePosition.X;

            double deltaY = currentMousePosition.Y - _resizeStartMousePosition.Y;


            double newX = _resizeStartX;

            double newY = _resizeStartY;

            double newWidth = _resizeStartWidth;

            double newHeight = _resizeStartHeight;


            if (_resizeDirection.HasFlag(ResizeDirection.Left))
            {
                newWidth = _resizeStartWidth - deltaX;

                if (newWidth < MinimumShapeWidth)
                {
                    newWidth = MinimumShapeWidth;

                    newX = _resizeStartX + (_resizeStartWidth - MinimumShapeWidth);
                }
                else
                {
                    newX = _resizeStartX + deltaX;
                }
            }


            if (_resizeDirection.HasFlag(ResizeDirection.Right))
            {
                newWidth = Math.Max(MinimumShapeWidth, _resizeStartWidth + deltaX);
            }


            if (_resizeDirection.HasFlag(ResizeDirection.Top))
            {
                newHeight = _resizeStartHeight - deltaY;

                if (newHeight < MinimumShapeHeight)
                {
                    newHeight = MinimumShapeHeight;

                    newY = _resizeStartY + (_resizeStartHeight - MinimumShapeHeight);
                }
                else
                {
                    newY = _resizeStartY + deltaY;
                }
            }


            if (_resizeDirection.HasFlag(ResizeDirection.Bottom))
            {
                newHeight = Math.Max(MinimumShapeHeight, _resizeStartHeight + deltaY);
            }


            shape.Width = newWidth;

            shape.Height = newHeight;


            Canvas.SetLeft(shape, newX);

            Canvas.SetTop(shape, newY);


            if (shape.Tag is ShapeElement model)
            {
                model.X = newX;
                model.Y = newY;

                model.Width = newWidth;
                model.Height = newHeight;
            }

            UpdateArrows();

            StatusText.Text = $"Größe: {newWidth:0} x {newHeight:0}";
        }


        // ============================================================
        // Resize beendet
        // ============================================================

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isResizing = false;


            if (sender is Thumb thumb &&
                thumb.Parent is Grid shape &&
                shape.Tag is ShapeElement model)
            {
                StatusText.Text = $"Shape-Größe: {model.Width:0} x {model.Height:0}";
            }
        }

        private void AddRectangle_Click(object sender, RoutedEventArgs e)
        {
            AddShape(ShapeType.Rectangle);
        }


        private void AddRoundedRectangle_Click(
            object sender,
            RoutedEventArgs e)
        {
            AddShape(ShapeType.RoundedRectangle);
        }


        private void AddEllipse_Click(object sender, RoutedEventArgs e)
        {
            AddShape(ShapeType.Ellipse);
        }


        private void AddDiamond_Click(object sender, RoutedEventArgs e)
        {
            AddShape(ShapeType.Diamond);
        }

        private void AddShape(ShapeType shapeType)
        {
            var shape = new ShapeElement
            {
                ShapeType = shapeType,

                X = _contextMenuPosition.X,
                Y = _contextMenuPosition.Y,

                Width = 160,
                Height = 90,

                Text = string.Empty
            };


            var control = CreateShapeControl(shape);

            WhiteBoardCanvas.Children.Add(control);

            SelectShape(control);

            StatusText.Text =  $"{GetShapeName(shapeType)} erstellt";

            WhiteBoardContextMenu.IsOpen = false;
        }

        private string GetShapeName(ShapeType shapeType)
        {
            return shapeType switch
            {
                ShapeType.Rectangle => "Rechteck",

                ShapeType.RoundedRectangle => "Abgerundetes Rechteck",

                ShapeType.Ellipse => "Ellipse",

                ShapeType.Diamond => "Raute",

                _ => "Shape"
            };
        }

        private FrameworkElement CreateShapeVisual(ShapeElement shape)
        {
            switch (shape.ShapeType)
            {
                case ShapeType.Rectangle:
                    {
                        return new Border
                        {
                            Background = Brushes.White,

                            BorderBrush =
                                Brushes.DimGray,

                            BorderThickness =
                                new Thickness(2),

                            CornerRadius =
                                new CornerRadius(0),

                            IsHitTestVisible = true
                        };
                    }


                case ShapeType.RoundedRectangle:
                    {
                        return new Border
                        {
                            Background = Brushes.White,

                            BorderBrush =
                                Brushes.DimGray,

                            BorderThickness =
                                new Thickness(2),

                            CornerRadius =
                                new CornerRadius(15),

                            IsHitTestVisible = true
                        };
                    }


                case ShapeType.Ellipse:
                    {
                        return new System.Windows.Shapes.Ellipse
                        {
                            Fill = Brushes.White,

                            Stroke =
                                Brushes.DimGray,

                            StrokeThickness = 2,

                            IsHitTestVisible = true
                        };
                    }


                case ShapeType.Diamond:
                    {
                        return new System.Windows.Shapes.Polygon
                        {
                            Fill = Brushes.White,

                            Stroke = Brushes.DimGray,

                            StrokeThickness = 2,

                            Points = new PointCollection
                                        {
                                            new Point(0.5, 0),
                                            new Point(1, 0.5),
                                            new Point(0.5, 1),
                                            new Point(0, 0.5)
                                        },

                            Stretch = Stretch.Fill,

                            IsHitTestVisible = true
                        };
                    }


                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetShapeSelectedVisual(Grid shape, bool selected)
        {
            if (shape.Children.Count == 0)
                return;


            if (shape.Children[0] is Border border)
            {
                border.BorderBrush = selected ? Brushes.DodgerBlue : Brushes.DimGray;

                border.BorderThickness = selected ? new Thickness(3) : new Thickness(2);

                return;
            }


            if (shape.Children[0] is System.Windows.Shapes.Shape vectorShape)
            {
                vectorShape.Stroke = selected ? Brushes.DodgerBlue : Brushes.DimGray;

                vectorShape.StrokeThickness = selected ? 3 : 2;
            }
        }

        // ============================================================
        // Auswahl
        // ============================================================

        private void SelectShape(Grid? shape)
        {
            // Pfeilauswahl aufheben
            if (_selectedArrow != null)
            {
                SelectArrow(null);
            }

            // Alte Auswahl entfernen

            if (_selectedShape != null)
            {
                SetShapeSelectedVisual(_selectedShape, false);

                SetResizeHandlesVisibility(_selectedShape, Visibility.Collapsed);
            }


            // --------------------------------------------------------
            // Neue Auswahl
            // --------------------------------------------------------

            _selectedShape = shape;


            if (_selectedShape != null)
            {
                SetShapeSelectedVisual(_selectedShape, true);

                SetResizeHandlesVisibility(_selectedShape, Visibility.Visible);

                Panel.SetZIndex(_selectedShape, GetHighestZIndex() + 1);
            }
        }

        // ============================================================
        // Resize-Griffe anzeigen/verstecken
        // ============================================================

        private void SetResizeHandlesVisibility(Grid shape, Visibility visibility)
        {
            foreach (UIElement child in shape.Children)
            {
                if (child is Thumb thumb)
                {
                    thumb.Visibility = visibility;
                }
            }
        }


        // ============================================================
        // Höchsten ZIndex ermitteln
        // ============================================================

        private int GetHighestZIndex()
        {
            int highest = 0;

            foreach (UIElement element in WhiteBoardCanvas.Children)
            {
                int zIndex = Panel.GetZIndex(element);

                if (zIndex > highest)
                    highest = zIndex;
            }

            return highest;
        }


        // ============================================================
        // Neues Board
        // ============================================================

        private void NewBoard_Click( object sender, RoutedEventArgs e)
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

            StatusText.Text = "Neues Whiteboard erstellt";
        }


        // ============================================================
        // Beenden
        // ============================================================

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        // ============================================================
        // Löschen
        // ============================================================

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShape == null)
            {
                StatusText.Text = "Kein Shape ausgewählt";

                return;
            }


            if (_selectedShape.Tag is not ShapeElement shapeModel)
            {
                return;
            }


            Guid shapeId = shapeModel.Id;


            // ========================================================
            // Alle Pfeile ermitteln, die mit dem Shape verbunden sind
            // ========================================================

            var connectedArrows = _arrows
                .Where(arrow =>
                    arrow.SourceId == shapeId ||
                    arrow.TargetId == shapeId)
                .ToList();


            // ========================================================
            // Pfeildarstellungen aus dem Canvas entfernen
            // ========================================================

            foreach (var element in WhiteBoardCanvas.Children
                         .OfType<System.Windows.Shapes.Path>()
                         .ToList())
            {
                if (element.Tag is not ArrowElement arrow)
                    continue;


                if (arrow.SourceId == shapeId ||
                    arrow.TargetId == shapeId)
                {
                    WhiteBoardCanvas.Children.Remove(element);
                }
            }


            // ========================================================
            // Pfeile aus dem Datenmodell entfernen
            // ========================================================

            foreach (var arrow in connectedArrows)
            {
                _arrows.Remove(arrow);
            }


            // ========================================================
            // Eventuelle Pfeilauswahl entfernen
            // ========================================================

            _selectedArrow = null;


            // ========================================================
            // Shape entfernen
            // ========================================================

            WhiteBoardCanvas.Children.Remove(_selectedShape);


            _selectedShape = null;


            StatusText.Text = "Shape und verbundene Pfeile gelöscht";
        }

        // ============================================================
        // Board leeren
        // ============================================================

        private void ClearBoard_Click(object sender, RoutedEventArgs e)
        {
            ClearBoard();

            StatusText.Text = "Whiteboard geleert";
        }


        private void ResetBoard_Click(object sender, RoutedEventArgs e)
        {
            ClearBoard();

            StatusText.Text = "Whiteboard zurückgesetzt";
        }


        private void ClearBoard()
        {
            _isDragging = false;

            _isResizing = false;

            _editingTextBox = null;

            _selectedShape = null;

            _selectedArrow = null;

            _isCreatingArrow = false;

            _arrowSourceShape = null;

            _arrows.Clear();

            WhiteBoardCanvas.Children.Clear();
        }

        // ============================================================
        // Contextmenü
        // ============================================================

        private void CloseContextMenu_Click(object sender, RoutedEventArgs e)
        {
            WhiteBoardContextMenu.IsOpen = false;
        }


        // ============================================================
        // Text - noch kein eigenständiges Element
        // ============================================================

        private void AddText_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplemented(
                "Text",
                "Eigenständige Texte werden in einem späteren Schritt " +
                "implementiert.");
        }


        // ============================================================
        // Pfeil
        // ============================================================

        private void AddArrow_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShape == null)
            {
                MessageBox.Show(
                    "Bitte zuerst ein Shape auswählen, von dem " +
                    "der Pfeil ausgehen soll.",
                    "Pfeil erstellen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            _arrowSourceShape = _selectedShape;

            _isCreatingArrow = true;


            WhiteBoardContextMenu.IsOpen = false;


            StatusText.Text = "Pfeil: Ziel-Shape auswählen";
        }


        // ============================================================
        // Noch nicht implementiert
        // ============================================================

        private void ShowNotImplemented(string elementName,string message)
        {
            MessageBox.Show(message, elementName, MessageBoxButton.OK, MessageBoxImage.Information);

            StatusText.Text = $"{elementName}: Funktion folgt später";
        }

        // ============================================================
        // Laden und speichern
        // ============================================================
        private void SaveBoard_Click(object sender, RoutedEventArgs e)
        {
            var dialog =
                new SaveFileDialog
                {
                    Title =
                        "Whiteboard speichern",

                    Filter =
                        "Whiteboard (*.json)|*.json|" +
                        "Alle Dateien (*.*)|*.*",

                    DefaultExt =
                        ".json",

                    AddExtension =
                        true
                };


            if (dialog.ShowDialog() != true)
                return;


            try
            {
                SaveBoard(
                    dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Das Whiteboard konnte nicht gespeichert werden.\n\n" +
                    $"{ex.Message}",
                    "Fehler beim Speichern",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveBoard(string fileName)
        {
            var document =
                new WhiteBoardDocument
                {
                    Version = 1,

                    Shapes =
                        GetShapeModels(),

                    Arrows =
                        _arrows.ToList()
                };


            var options =
                CreateJsonOptions();


            string json =
                JsonSerializer.Serialize(
                    document,
                    options);


            File.WriteAllText(
                fileName,
                json);


            StatusText.Text = $"Board gespeichert: {fileName}";
        }

        private List<ShapeElement> GetShapeModels()
        {
            return WhiteBoardCanvas.Children
                .OfType<Grid>()
                .Where(grid =>
                    grid.Tag is ShapeElement)
                .Select(grid =>
                    (ShapeElement)grid.Tag)
                .ToList();
        }

        private void LoadBoard_Click(object sender, RoutedEventArgs e)
        {
            var dialog =
                new OpenFileDialog
                {
                    Title =
                        "Whiteboard laden",

                    Filter =
                        "Whiteboard (*.json)|*.json|" +
                        "Alle Dateien (*.*)|*.*",

                    DefaultExt =
                        ".json"
                };


            if (dialog.ShowDialog() != true)
                return;


            try
            {
                LoadBoard(
                    dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Die Whiteboard-Datei konnte nicht geladen werden.\n\n" +
                    $"{ex.Message}",
                    "Fehler beim Laden",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadBoard(string fileName)
        {
            string json = File.ReadAllText(fileName);


            var options = CreateJsonOptions();


            var document = JsonSerializer.Deserialize<WhiteBoardDocument>(json, options);


            if (document == null)
                throw new InvalidOperationException("Die Whiteboard-Datei konnte nicht gelesen werden.");


            ClearBoard();


            foreach (ShapeElement shape in document.Shapes)
            {
                AddLoadedShape(shape);
            }


            foreach (ArrowElement arrow in document.Arrows)
            {
                _arrows.Add(arrow);
            }


            foreach (ArrowElement arrow in _arrows)
            {
                DrawArrow(arrow);
            }

            SelectShape(null);
            SelectArrow(null);

            StatusText.Text = $"Board geladen: {fileName}";
        }

        private void AddLoadedShape(ShapeElement shape)
        {
            var control = CreateShapeControl(shape);


            Canvas.SetLeft(control, shape.X);
            Canvas.SetTop(control, shape.Y);


            WhiteBoardCanvas.Children.Add(control);
        }

        // ============================================================
        // JSON Optionen
        // ============================================================
        private JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,

                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            };
        }

        // ============================================================
        // Resize-Richtungen
        // ============================================================

        [Flags]
        private enum ResizeDirection
        {
            None = 0,

            Left = 1,

            Right = 2,

            Top = 4,

            Bottom = 8,

            TopLeft = Top | Left,

            TopRight = Top | Right,

            BottomLeft = Bottom | Left,

            BottomRight = Bottom | Right
        }
    }
}