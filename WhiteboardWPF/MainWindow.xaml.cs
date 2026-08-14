namespace WhiteboardWPF
{
    using System.IO;
    using System.Text.Json;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    using Microsoft.Win32;

    using WhiteboardWPF.Models;
    using WhiteboardWPF.ShapeProvider;

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
        // Text-Elemente
        // ============================================================
        private readonly List<TextElement> _textElements = new();
        private Grid? _selectedTextElement;
        private bool _isDraggingText;
        private Point _textDragStartMousePosition;
        private double _textDragStartX;
        private double _textDragStartY;

        // ============================================================
        // Resize
        // ============================================================
        private const double MinimumShapeWidth = 40;
        private const double MinimumShapeHeight = 30;
        private bool _isResizing;
        private ResizeDirection _resizeDirection;
        //private Point _resizeStartMousePosition;
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


        // ============================================================
        // Mehrfachmarkierung
        // ============================================================
        private readonly List<Grid> _selectedShapes = new();
        private readonly Dictionary<Grid, Point> _multiDragStartPositions = new();
        private readonly List<Grid> _selectedTextElements = new();

        public MainWindow()
        {
            this.InitializeComponent();
            this.InitializeShapeMenu();
            StatusText.Text = "Whiteboard bereit";
        }


        // ============================================================
        // Whiteboard - rechte Maustaste
        // ============================================================

        #region Klick Events 
        /// <summary>
        /// Whiteboard - rechte Maustaste
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WhiteBoardCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _contextMenuPosition = e.GetPosition(WhiteBoardCanvas);

            StatusText.Text = $"Position: X={_contextMenuPosition.X:0}, Y={_contextMenuPosition.Y:0}";
        }

        /// <summary>
        /// Whiteboard - linke Maustaste
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WhiteBoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectShape(null);
        }

        /// <summary>
        /// Shape erstellen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Contextmenü für Shape
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Text über Contextmenü bearbeiten
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Text über Doppelklick bearbeiten
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShapeText_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            if (textBox.Parent is not Grid shape)
                return;


            SelectShape(shape);

            BeginTextEditing(shape);

            e.Handled = true;
        }

        #endregion Klick Events 

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

            var shapeVisual = CreateShapeVisual(shape);

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


        /// <summary>
        /// Textbearbeitung starten
        /// </summary>
        /// <param name="shape"></param>
        private void BeginTextEditing(Grid shape)
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

        /// <summary>
        /// TextBox finden
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        private TextBox? FindTextBox(Grid shape)
        {
            foreach (UIElement child in shape.Children)
            {
                if (child is TextBox textBox)
                    return textBox;
            }

            return null;
        }


        /// <summary>
        /// Tastatur in TextBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShapeTextBox_KeyDown(object sender, KeyEventArgs e)
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

        /// <summary>
        /// Textbearbeitung beendet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void ShapeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;


            if (_editingTextBox != textBox)
                return;


            FinishTextEditing(textBox);
        }


        /// <summary>
        /// Text übernehmen
        /// </summary>
        /// <param name="textBox"></param>

        private void FinishTextEditing(TextBox textBox)
        {
            if (textBox.Parent is not Grid shape)
                return;


            if (shape.Tag is not ShapeElement model)
                return;


            model.Text =
                textBox.Text;


            textBox.IsReadOnly = true;


            _editingTextBox = null;


            StatusText.Text = "Text übernommen";
        }


        /// <summary>
        /// Textbearbeitung abbrechen
        /// </summary>
        /// <param name="textBox"></param>

        private void CancelTextEditing(TextBox textBox)
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


            StatusText.Text = "Textbearbeitung abgebrochen";
        }


        /// <summary>
        /// Resize Thumb hinzufügen
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="horizontalAlignment"></param>
        /// <param name="verticalAlignment"></param>
        /// <param name="direction"></param>
        private void AddResizeThumb(Grid grid, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, ResizeDirection direction)
        {
            var thumb = new Thumb
            {
                Width = 10,
                Height = 10,

                HorizontalAlignment = horizontalAlignment,

                VerticalAlignment = verticalAlignment,

                Background = Brushes.White,
                BorderBrush = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),

                Cursor = GetResizeCursor(direction),

                Tag = direction,

                Visibility = Visibility.Collapsed
            };


            thumb.DragStarted += ResizeThumb_DragStarted;
            thumb.DragDelta += ResizeThumb_DragDelta;
            thumb.DragCompleted += ResizeThumb_DragCompleted;


            grid.Children.Add(thumb);
        }


        // ============================================================
        // Resize Cursor
        // ============================================================

        private Cursor GetResizeCursor(ResizeDirection direction)
        {
            return direction switch
            {
                ResizeDirection.TopLeft => Cursors.SizeNWSE,

                ResizeDirection.Top => Cursors.SizeNS,

                ResizeDirection.TopRight => Cursors.SizeNESW,

                ResizeDirection.Left => Cursors.SizeWE,

                ResizeDirection.Right => Cursors.SizeWE,

                ResizeDirection.BottomLeft => Cursors.SizeNESW,

                ResizeDirection.Bottom => Cursors.SizeNS,

                ResizeDirection.BottomRight => Cursors.SizeNWSE,

                _ => Cursors.Arrow
            };
        }

        /// <summary>
        /// Shape auswählen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Shape_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Grid shape)
                return;


            // ========================================================
            // Pfeil erstellen
            // ========================================================

            if (_isCreatingArrow)
            {
                CreateArrow(_arrowSourceShape, shape);

                e.Handled = true;

                return;
            }


            // ========================================================
            // Resize-Griffe
            // ========================================================

            if (IsResizeThumbSource(e.OriginalSource as DependencyObject))
            {
                return;
            }

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


            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // ========================================================
                // Strg + Klick
                // ========================================================

                if (IsShapeSelected(shape))
                {
                    RemoveShapeFromSelection(shape);

                    _selectedShape = _selectedShapes.LastOrDefault();
                }
                else
                {
                    AddShapeToSelection(shape);

                    _selectedShape = shape;
                }
            }
            else
            {
                // ========================================================
                // Normaler Klick
                // ========================================================

                // Wenn bereits mehrere Shapes ausgewählt sind und
                // auf eines dieser Shapes geklickt wird, soll die
                // bestehende Mehrfachauswahl erhalten bleiben.
                if (_selectedShapes.Count > 1 && IsShapeSelected(shape))
                {
                    _selectedShape = shape;
                }
                else
                {
                    // Normaler Einzelklick auf ein nicht ausgewähltes Shape
                    SelectSingleShape(shape);
                }
            }

            _isDragging = true;

            _dragStartMousePosition = e.GetPosition(WhiteBoardCanvas);

            _dragStartShapeX = Canvas.GetLeft(shape);

            _dragStartShapeY = Canvas.GetTop(shape);

            if (_selectedShapes.Count > 1)
            {
                StartMultiDrag();
            }

            shape.CaptureMouse();

            e.Handled = true;
        }

        private void CreateArrow(Grid? sourceShape, Grid targetShape)
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


            StatusText.Text = "Pfeil erstellt";
        }

        private void DrawArrow(ArrowElement arrow)
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


            Point start = GetConnectionPoint(sourceShape, targetShape);
            Point end = GetConnectionPoint(targetShape, sourceShape);

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

        private Point GetConnectionPoint(Grid source, Grid target)
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


            Vector direction = targetCenter - sourceCenter;


            if (direction.Length < 0.001)
                return sourceCenter;


            direction.Normalize();


            return sourceModel.ShapeType switch
            {
                ShapeType.Rectangle => GetRectangleConnectionPoint(sourceModel, sourceCenter, direction),

                ShapeType.RoundedRectangle => GetRectangleConnectionPoint(sourceModel, sourceCenter, direction),

                ShapeType.Ellipse => GetEllipseConnectionPoint(sourceModel, sourceCenter, direction),

                ShapeType.Diamond => GetDiamondConnectionPoint(sourceModel, sourceCenter, direction),

                ShapeType.Triangle => GetTriangleConnectionPoint(sourceModel, sourceCenter, direction),
                ShapeType.Hexagon => GetHexagonConnectionPoint(sourceModel, sourceCenter, direction),

                _ => sourceCenter
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

        private Point GetEllipseConnectionPoint(ShapeElement shape, Point center, Vector direction)
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

        private Point GetDiamondConnectionPoint(ShapeElement shape, Point center, Vector direction)
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

        private Point GetTriangleConnectionPoint(ShapeElement shape, Point center, Vector direction)
        {
            if (direction.Length < 0.001)
                return center;


            direction.Normalize();


            // --------------------------------------------------------
            // Eckpunkte des Dreiecks
            //
            //              Top
            //               /\
            //              /  \
            //             /    \
            //            /      \
            //           /________\
            //      BottomLeft   BottomRight
            // --------------------------------------------------------

            Point top =
                new Point(
                    shape.X +
                        shape.Width / 2,

                    shape.Y);


            Point bottomLeft =
                new Point(
                    shape.X,

                    shape.Y +
                        shape.Height);


            Point bottomRight =
                new Point(
                    shape.X +
                        shape.Width,

                    shape.Y +
                        shape.Height);


            Point rayEnd =
                center +
                direction * 10000;


            Point? intersection;


            // --------------------------------------------------------
            // Linke Seite
            // --------------------------------------------------------

            intersection = GetLineIntersection(center, rayEnd, top, bottomLeft);

            if (intersection.HasValue)
                return intersection.Value;


            // --------------------------------------------------------
            // Rechte Seite
            // --------------------------------------------------------

            intersection =
                GetLineIntersection(
                    center,
                    rayEnd,
                    top,
                    bottomRight);

            if (intersection.HasValue)
                return intersection.Value;


            // --------------------------------------------------------
            // Untere Seite
            // --------------------------------------------------------

            intersection = GetLineIntersection(center, rayEnd, bottomLeft, bottomRight);

            if (intersection.HasValue)
                return intersection.Value;


            return center;
        }

        private Point GetHexagonConnectionPoint(ShapeElement shape, Point center, Vector direction)
        {
            // zunächst Bounding-Box-Verbindung
            return GetRectangleConnectionPoint(shape, center, direction);
        }

        private Point? GetLineIntersection(Point line1Start, Point line1End, Point line2Start, Point line2End)
        {
            double x1 = line1Start.X;
            double y1 = line1Start.Y;

            double x2 = line1End.X;
            double y2 = line1End.Y;

            double x3 = line2Start.X;
            double y3 = line2Start.Y;

            double x4 = line2End.X;
            double y4 = line2End.Y;


            double denominator =
                (x1 - x2) * (y3 - y4)
                -
                (y1 - y2) * (x3 - x4);


            if (Math.Abs(denominator) < 0.000001)
                return null;


            double t =
                ((x1 - x3) * (y3 - y4)
                -
                (y1 - y3) * (x3 - x4))
                /
                denominator;


            double u =
                -(
                    (x1 - x2) * (y1 - y3)
                    -
                    (y1 - y2) * (x1 - x3)
                )
                /
                denominator;


            if (t < 0 ||
                t > 1 ||
                u < 0 ||
                u > 1)
            {
                return null;
            }


            return new Point(
                x1 +
                    t * (x2 - x1),

                y1 +
                    t * (y2 - y1));
        }

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


            WhiteBoardCanvas.Children.Remove(path);


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
            if (sender is not Grid shape)
                return;


            if (!_isDragging)
                return;


            if (_isResizing)
                return;


            Point currentPosition =
                e.GetPosition(
                    WhiteBoardCanvas);


            double deltaX =
                currentPosition.X -
                _dragStartMousePosition.X;


            double deltaY =
                currentPosition.Y -
                _dragStartMousePosition.Y;


            // ========================================================
            // Mehrfachauswahl
            // ========================================================

            if (_selectedShapes.Count > 1)
            {
                MoveSelectedShapes(deltaX, deltaY);

                e.Handled = true;

                return;
            }


            // ========================================================
            // Einzelnes Shape
            // ========================================================

            double newX =
                _dragStartShapeX +
                deltaX;


            double newY =
                _dragStartShapeY +
                deltaY;


            Canvas.SetLeft(
                shape,
                newX);


            Canvas.SetTop(
                shape,
                newY);


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


            if (_isResizing)
                return;


            if (!_isDragging)
                return;


            _isDragging = false;


            if (shape.IsMouseCaptured)
            {
                shape.ReleaseMouseCapture();
            }


            _multiDragStartPositions.Clear();


            if (shape.Tag is ShapeElement model)
            {
                StatusText.Text = $"Shape positioniert: " + $"X={model.X:0}, " + $"Y={model.Y:0}";
            }


            e.Handled = true;
        }


        // ============================================================
        // Resize gestartet
        // ============================================================

        private void ResizeThumb_DragStarted(
            object sender,
            DragStartedEventArgs e)
        {
            if (sender is not Thumb thumb)
                return;

            if (thumb.Parent is not Grid control)
                return;

            if (thumb.Tag is not ResizeDirection direction)
                return;


            _isDragging = false;
            _isDraggingText = false;

            // ========================================================
            // Resize starten
            // ========================================================
            _isResizing = true;
            _resizeDirection = direction;


            // ========================================================
            // Shape
            // ========================================================

            if (control.Tag is ShapeElement shape)
            {
                SelectShape(control);

                _resizeStartX = shape.X;
                _resizeStartY = shape.Y;

                _resizeStartWidth = shape.Width;
                _resizeStartHeight = shape.Height;

                StatusText.Text = "Shape-Größe ändern";

                return;
            }


            // ========================================================
            // Text
            // ========================================================

            if (control.Tag is TextElement text)
            {
                _selectedTextElement = control;

                _resizeStartX = text.X;
                _resizeStartY = text.Y;

                _resizeStartWidth = text.Width;
                _resizeStartHeight = text.Height;

                StatusText.Text = "Text-Größe ändern";

                return;
            }


            _isResizing = false;
        }


        // ============================================================
        // Resize
        // ============================================================
        private void ResizeThumb_DragDelta(
            object sender,
            DragDeltaEventArgs e)
        {
            if (!_isResizing)
                return;

            if (sender is not Thumb thumb)
                return;

            if (thumb.Parent is not Grid control)
                return;


            double deltaX =
                e.HorizontalChange;

            double deltaY =
                e.VerticalChange;


            double newX =
                _resizeStartX;

            double newY =
                _resizeStartY;

            double newWidth =
                _resizeStartWidth;

            double newHeight =
                _resizeStartHeight;


            // ========================================================
            // Links
            // ========================================================

            if (_resizeDirection.HasFlag(
                    ResizeDirection.Left))
            {
                newWidth =
                    _resizeStartWidth - deltaX;


                if (newWidth < MinimumShapeWidth)
                {
                    newWidth =
                        MinimumShapeWidth;

                    newX =
                        _resizeStartX +
                        (_resizeStartWidth -
                         MinimumShapeWidth);
                }
                else
                {
                    newX =
                        _resizeStartX +
                        deltaX;
                }
            }


            // ========================================================
            // Rechts
            // ========================================================

            if (_resizeDirection.HasFlag(
                    ResizeDirection.Right))
            {
                newWidth =
                    Math.Max(
                        MinimumShapeWidth,
                        _resizeStartWidth + deltaX);
            }


            // ========================================================
            // Oben
            // ========================================================

            if (_resizeDirection.HasFlag(
                    ResizeDirection.Top))
            {
                newHeight =
                    _resizeStartHeight - deltaY;


                if (newHeight < MinimumShapeHeight)
                {
                    newHeight =
                        MinimumShapeHeight;

                    newY =
                        _resizeStartY +
                        (_resizeStartHeight -
                         MinimumShapeHeight);
                }
                else
                {
                    newY =
                        _resizeStartY +
                        deltaY;
                }
            }


            // ========================================================
            // Unten
            // ========================================================

            if (_resizeDirection.HasFlag(
                    ResizeDirection.Bottom))
            {
                newHeight =
                    Math.Max(
                        MinimumShapeHeight,
                        _resizeStartHeight + deltaY);
            }


            // ========================================================
            // Control
            // ========================================================

            control.Width = newWidth;
            control.Height = newHeight;

            Canvas.SetLeft(control, newX);
            Canvas.SetTop(control, newY);


            // ========================================================
            // Shape
            // ========================================================

            if (control.Tag is ShapeElement shape)
            {
                shape.X = newX;
                shape.Y = newY;

                shape.Width = newWidth;
                shape.Height = newHeight;

                UpdateArrows();
            }


            // ========================================================
            // Text
            // ========================================================

            else if (control.Tag is TextElement text)
            {
                text.X = newX;
                text.Y = newY;

                text.Width = newWidth;
                text.Height = newHeight;
            }


            StatusText.Text =
                $"Größe: {newWidth:0} x {newHeight:0}";
        }

        // ============================================================
        // Resize beendet
        // ============================================================

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isResizing = false;


            if (sender is Thumb thumb &&
                thumb.Parent is Grid control)
            {
                if (control.Tag is ShapeElement shape)
                {
                    StatusText.Text = $"Shape-Größe: {shape.Width:0} x {shape.Height:0}";
                }
                else if (control.Tag is TextElement text)
                {
                    StatusText.Text = $"Text-Größe: {text.Width:0} x {text.Height:0}";
                }
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
            return ShapeDefinitionProvider.GetDefinition(shapeType) ?.Name ?? "Shape";
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
                case ShapeType.Triangle:
                    {
                        return CreateTriangleVisual(shape);
                    }

                case ShapeType.Hexagon:
                    {
                        return CreateHexagonVisual(shape);
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
            // ========================================================
            // Textauswahl aufheben
            // ========================================================

            foreach (Grid text in _selectedTextElements.ToList())
            {
                SetResizeHandlesVisibility(text,  Visibility.Collapsed);
            }

            _selectedTextElements.Clear();

            _selectedTextElement = null;

            // ========================================================
            // Pfeilauswahl aufheben
            // ========================================================

            if (_selectedArrow != null)
            {
                SelectArrow(null);
            }


            // ========================================================
            // Textauswahl aufheben
            // ========================================================

            if (_selectedTextElement != null)
            {
                SetResizeHandlesVisibility(_selectedTextElement, Visibility.Collapsed);

                _selectedTextElement = null;
            }


            // ========================================================
            // Alte Shape-Auswahl entfernen
            // ========================================================

            if (_selectedShape != null)
            {
                SetShapeSelectedVisual(_selectedShape, false);

                SetResizeHandlesVisibility(_selectedShape, Visibility.Collapsed);
            }


            // ========================================================
            // Neue Auswahl
            // ========================================================

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
            // ========================================================
            // Mehrfachauswahl
            // ========================================================

            if (_selectedShapes.Count > 0)
            {
                var shapesToDelete =
                    _selectedShapes
                        .Where(shape =>
                            shape.Tag is ShapeElement)
                        .ToList();


                foreach (Grid shapeControl in shapesToDelete)
                {
                    if (shapeControl.Tag is not ShapeElement shapeModel)
                        continue;


                    Guid shapeId =
                        shapeModel.Id;


                    // ------------------------------------------------
                    // Verbundene Pfeile ermitteln
                    // ------------------------------------------------

                    var connectedArrows =
                        _arrows
                            .Where(arrow =>
                                arrow.SourceId == shapeId ||
                                arrow.TargetId == shapeId)
                            .ToList();


                    // ------------------------------------------------
                    // Pfeildarstellungen entfernen
                    // ------------------------------------------------

                    foreach (var element in
                             WhiteBoardCanvas.Children
                                 .OfType<System.Windows.Shapes.Path>()
                                 .ToList())
                    {
                        if (element.Tag is not ArrowElement arrow)
                            continue;


                        if (arrow.SourceId == shapeId ||
                            arrow.TargetId == shapeId)
                        {
                            WhiteBoardCanvas.Children.Remove(
                                element);
                        }
                    }


                    // ------------------------------------------------
                    // Pfeile aus Datenmodell entfernen
                    // ------------------------------------------------

                    foreach (var arrow in connectedArrows)
                    {
                        _arrows.Remove(arrow);
                    }


                    // ------------------------------------------------
                    // Shape entfernen
                    // ------------------------------------------------

                    WhiteBoardCanvas.Children.Remove(
                        shapeControl);
                }


                // ----------------------------------------------------
                // Auswahl zurücksetzen
                // ----------------------------------------------------

                _selectedShapes.Clear();

                _selectedShape = null;

                _selectedArrow = null;


                StatusText.Text =
                    $"{shapesToDelete.Count} Shapes gelöscht";

                return;
            }


            // ========================================================
            // Einzelnes Shape
            // ========================================================

            if (_selectedShape == null)
            {
                StatusText.Text =
                    "Kein Shape ausgewählt";

                return;
            }


            if (_selectedShape.Tag is not ShapeElement shapeModelSingle)
            {
                return;
            }


            Guid shapeIdSingle =
                shapeModelSingle.Id;


            // ========================================================
            // Verbundene Pfeile
            // ========================================================

            var connectedArrowsSingle =
                _arrows
                    .Where(arrow =>
                        arrow.SourceId == shapeIdSingle ||
                        arrow.TargetId == shapeIdSingle)
                    .ToList();


            // ========================================================
            // Pfeildarstellungen entfernen
            // ========================================================

            foreach (var element in
                     WhiteBoardCanvas.Children
                         .OfType<System.Windows.Shapes.Path>()
                         .ToList())
            {
                if (element.Tag is not ArrowElement arrow)
                    continue;


                if (arrow.SourceId == shapeIdSingle ||
                    arrow.TargetId == shapeIdSingle)
                {
                    WhiteBoardCanvas.Children.Remove(
                        element);
                }
            }


            // ========================================================
            // Pfeile aus Datenmodell entfernen
            // ========================================================

            foreach (var arrow in connectedArrowsSingle)
            {
                _arrows.Remove(arrow);
            }


            _selectedArrow = null;


            // ========================================================
            // Shape entfernen
            // ========================================================

            WhiteBoardCanvas.Children.Remove(
                _selectedShape);


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
            AddTextElement();
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
                    Title = "Whiteboard speichern",
                    Filter = "Whiteboard (*.json)|*.json|" + "Alle Dateien (*.*)|*.*",
                    DefaultExt = ".json",
                    AddExtension = true
                };


            if (dialog.ShowDialog() != true)
                return;


            try
            {
                SaveBoard(dialog.FileName);
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
            UpdateTextElementsFromControls();

            var document =
                new WhiteBoardDocument
                {
                    Version = 1,
                    Shapes = GetShapeModels(),
                    TextElements = _textElements.ToList(),
                    Arrows = _arrows.ToList()
                };

            var options = CreateJsonOptions();
            string json = JsonSerializer.Serialize(document, options);
            File.WriteAllText(fileName, json);
            StatusText.Text = $"Board gespeichert: {fileName}";
        }

        private List<ShapeElement> GetShapeModels()
        {
            return WhiteBoardCanvas.Children.OfType<Grid>().Where(grid => grid.Tag is ShapeElement).Select(grid => (ShapeElement)grid.Tag).ToList();
        }

        private void LoadBoard_Click(object sender, RoutedEventArgs e)
        {
            var dialog =
                new OpenFileDialog
                {
                    Title = "Whiteboard laden",
                    Filter = "Whiteboard (*.json)|*.json|" + "Alle Dateien (*.*)|*.*",
                    DefaultExt = ".json"
                };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                LoadBoard(dialog.FileName);
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


            var document =
                JsonSerializer.Deserialize<WhiteBoardDocument>(
                    json,
                    options);


            if (document == null)
                throw new InvalidOperationException("Die Whiteboard-Datei konnte nicht gelesen werden.");


            ClearBoard();


            // ========================================================
            // Shapes
            // ========================================================

            foreach (ShapeElement shape in document.Shapes)
            {
                AddLoadedShape(shape);
            }


            // ========================================================
            // Text-Elemente
            // ========================================================

            _textElements.Clear();


            foreach (TextElement text in document.TextElements)
            {
                _textElements.Add(text);

                var control = CreateTextControl(text);

                WhiteBoardCanvas.Children.Add(control);
            }


            // ========================================================
            // Pfeile
            // ========================================================

            foreach (ArrowElement arrow in document.Arrows)
            {
                _arrows.Add(arrow);
            }


            foreach (ArrowElement arrow in _arrows)
            {
                DrawArrow(arrow);
            }


            // ========================================================
            // Auswahl zurücksetzen
            // ========================================================

            SelectShape(null);

            SelectTextElement(null);

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

                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }


        // ============================================================
        // Mehrfachmarkierung
        // ============================================================
        private bool IsShapeSelected(Grid shape)
        {
            return _selectedShapes.Contains(shape);
        }

        private void AddShapeToSelection(Grid shape)
        {
            if (_selectedShapes.Contains(shape))
                return;


            _selectedShapes.Add(shape);


            SetShapeSelectedVisual(
                shape,
                true);


            SetResizeHandlesVisibility(
                shape,
                Visibility.Visible);
        }

        private void RemoveShapeFromSelection(Grid shape)
        {
            if (!_selectedShapes.Remove(shape))
                return;


            SetShapeSelectedVisual(shape, false);

            SetResizeHandlesVisibility(shape, Visibility.Collapsed);
        }

        private void ClearShapeSelection()
        {
            foreach (Grid shape in _selectedShapes.ToList())
            {
                SetShapeSelectedVisual(shape, false);

                SetResizeHandlesVisibility(shape, Visibility.Collapsed);
            }


            _selectedShapes.Clear();

            _selectedShape = null;
        }

        private void SelectSingleShape(Grid shape)
        {
            ClearShapeSelection();
            AddShapeToSelection(shape);

            _selectedShape = shape;
        }

        private void StartMultiDrag()
        {
            _multiDragStartPositions.Clear();


            foreach (Grid shape in _selectedShapes)
            {
                _multiDragStartPositions[shape] = new Point(Canvas.GetLeft(shape), Canvas.GetTop(shape));
            }
        }

        private void MoveSelectedShapes(double deltaX, double deltaY)
        {
            foreach (Grid shape in _selectedShapes.ToList())
            {
                if (!_multiDragStartPositions.TryGetValue(
                        shape,
                        out Point start))
                {
                    continue;
                }

                double newX = start.X + deltaX;
                double newY = start.Y + deltaY;

                Canvas.SetLeft(shape, newX);
                Canvas.SetTop(shape, newY);


                if (shape.Tag is ShapeElement model)
                {
                    model.X = newX;
                    model.Y = newY;
                }
            }

            UpdateArrows();

            StatusText.Text = $"{_selectedShapes.Count} Shapes verschoben";
        }

        // ============================================================
        // Shape Contextmenu
        // ============================================================
        private void InitializeShapeMenu()
        {
            this.ShapeMenu.Items.Clear();


            foreach (ShapeDefinition definition
                     in ShapeDefinitionProvider.Definitions)
            {
                var item =
                    new MenuItem
                    {
                        Header = definition.Name,
                        Tag = definition.Type
                    };


                item.Click += AddShapeFromMenu_Click;

                this.ShapeMenu.Items.Add(item);
            }
        }

        private void AddShapeFromMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;


            if (item.Tag is not ShapeType shapeType)
                return;

            AddShape(shapeType);
        }

        // ============================================================
        // Shape Definition Dreieck
        // ============================================================

        private FrameworkElement CreateTriangleVisual(ShapeElement shape)
        {
            return new System.Windows.Shapes.Polygon
            {
                Fill = Brushes.White,

                Stroke = Brushes.DimGray,

                StrokeThickness = 2,

                Points = new PointCollection
                    {
                        new Point(0.5, 0),
                        new Point(1, 1),
                        new Point(0, 1)
                    },

                Stretch = Stretch.Fill,
                IsHitTestVisible = true
            };
        }

        // ============================================================
        // Shape Definition Hexagon
        // ============================================================
        private FrameworkElement CreateHexagonVisual(ShapeElement shape)
        {
            return new System.Windows.Shapes.Polygon
            {
                Fill = Brushes.White,
                Stroke = Brushes.DimGray,
                StrokeThickness = 2,
                Points = new PointCollection
                    {
                        new Point(0.25, 0),
                        new Point(0.75, 0),
                        new Point(1, 0.5),
                        new Point(0.75, 1),
                        new Point(0.25, 1),
                        new Point(0, 0.5)
                    },

                Stretch = Stretch.Fill,
                IsHitTestVisible = true
            };
        }

        // ============================================================
        // Resize-Funktion
        // ============================================================

        private bool IsResizeThumbSource(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is Thumb)
                    return true;

                source =
                    VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        // ============================================================
        // Text-Elemente
        // ============================================================
        private void AddTextElement()
        {
            var text = new TextElement
            {
                X = _contextMenuPosition.X,
                Y = _contextMenuPosition.Y,

                Width = 200,
                Height = 60,

                Text = $"Text-{DateTime.Now:HH:mm:ss}",

                FontSize = 16
            };


            _textElements.Add(text);

            var control = CreateTextControl(text);

            WhiteBoardCanvas.Children.Add(control);

            WhiteBoardContextMenu.IsOpen = false;

            StatusText.Text = "Text erstellt";
        }

        private Grid CreateTextControl(TextElement text)
        {
            var grid = new Grid
            {
                Width = text.Width,
                Height = text.Height,
                Tag = text
            };


            var textBox = new TextBox
            {
                Text = text.Text,
                FontSize = text.FontSize,
                AcceptsReturn = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment =  HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4),
                IsReadOnly = true,
                IsHitTestVisible = true,
                Cursor = Cursors.Arrow,
                Tag = text
            };

            textBox.KeyDown += TextElement_KeyDown;
            textBox.LostFocus += TextElement_LostFocus;

            var textContextMenu = new ContextMenu();

            var deleteMenuItem = new MenuItem
            {
                Header = "Löschen"
            };

            deleteMenuItem.Click += TextElement_DeleteClick;

            textContextMenu.Items.Add(deleteMenuItem);

            grid.ContextMenu = textContextMenu;

            grid.Children.Add(textBox);

            // --------------------------------------------------------
            // Verschieben
            // --------------------------------------------------------

            grid.PreviewMouseLeftButtonDown += TextElement_PreviewMouseLeftButtonDown;
            grid.PreviewMouseMove += TextElement_PreviewMouseMove;
            grid.PreviewMouseLeftButtonUp += TextElement_PreviewMouseLeftButtonUp;

            AddResizeThumb(grid, HorizontalAlignment.Left, VerticalAlignment.Top, ResizeDirection.TopLeft);
            AddResizeThumb(grid, HorizontalAlignment.Center, VerticalAlignment.Top, ResizeDirection.Top);
            AddResizeThumb(grid, HorizontalAlignment.Right, VerticalAlignment.Top, ResizeDirection.TopRight);
            AddResizeThumb(grid, HorizontalAlignment.Left, VerticalAlignment.Center, ResizeDirection.Left);
            AddResizeThumb(grid, HorizontalAlignment.Right, VerticalAlignment.Center, ResizeDirection.Right);
            AddResizeThumb(grid, HorizontalAlignment.Left, VerticalAlignment.Bottom, ResizeDirection.BottomLeft);
            AddResizeThumb(grid, HorizontalAlignment.Center, VerticalAlignment.Bottom, ResizeDirection.Bottom);
            AddResizeThumb(grid, HorizontalAlignment.Right, VerticalAlignment.Bottom, ResizeDirection.BottomRight);

            Canvas.SetLeft(grid, text.X);
            Canvas.SetTop(grid, text.Y);

            return grid;
        }

        private void TextElement_DeleteClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            if (menuItem.Parent is not ContextMenu contextMenu)
                return;

            if (contextMenu.PlacementTarget is not Grid grid)
                return;

            DeleteTextElement(grid);
        }

        private void DeleteTextElement(Grid textControl)
        {
            if (textControl.Tag is not TextElement text)
                return;


            // --------------------------------------------------------
            // Aus Datenmodell entfernen
            // --------------------------------------------------------

            _textElements.Remove(text);


            // --------------------------------------------------------
            // Auswahl entfernen
            // --------------------------------------------------------

            if (_selectedTextElement == textControl)
            {
                _selectedTextElement = null;
            }


            // --------------------------------------------------------
            // Control vom Canvas entfernen
            // --------------------------------------------------------

            WhiteBoardCanvas.Children.Remove(textControl);


            StatusText.Text = "Text gelöscht";
        }

        private void TextElement_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;


            textBox.IsReadOnly = true;

            textBox.Cursor = Cursors.Arrow;


            if (textBox.Tag is TextElement text)
            {
                text.Text = textBox.Text;
            }
        }

        private void TextElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;


            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                textBox.IsReadOnly = true;

                textBox.Cursor = Cursors.Arrow;

                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

                e.Handled = true;
            }


            if (e.Key == Key.Escape)
            {
                if (textBox.Tag is TextElement text)
                {
                    textBox.Text = text.Text;
                }


                textBox.IsReadOnly = true;

                textBox.Cursor =  Cursors.Arrow;

                e.Handled = true;
            }
        }

        private void TextElement_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Grid grid)
                return;


            if (grid.Tag is not TextElement text)
                return;


            // ========================================================
            // Resize-Griff
            // ========================================================

            if (IsResizeThumbSource(
                    e.OriginalSource as DependencyObject))
            {
                return;
            }


            // ========================================================
            // Doppelklick -> Text bearbeiten
            // ========================================================

            if (e.ClickCount >= 2)
            {
                SelectTextElement(grid);


                if (grid.Children
                    .OfType<TextBox>()
                    .FirstOrDefault() is TextBox textBox)
                {
                    textBox.IsReadOnly = false;

                    textBox.Cursor = Cursors.IBeam;

                    textBox.Focus();

                    textBox.SelectAll();
                }

                e.Handled = true;

                return;
            }


            // ========================================================
            // Strg -> Mehrfachauswahl
            // ========================================================

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (IsTextSelected(grid))
                {
                    RemoveTextFromSelection(grid);

                    _selectedTextElement = _selectedTextElements.LastOrDefault();
                }
                else
                {
                    AddTextToSelection(grid);

                    _selectedTextElement = grid;
                }

                e.Handled = true;

                return;
            }


            // ========================================================
            // Einfacher Klick -> Einzelauswahl
            // ========================================================

            SelectTextElement(grid);


            _isDraggingText = true;

            _textDragStartMousePosition = e.GetPosition(WhiteBoardCanvas);

            _textDragStartX = text.X;
            _textDragStartY = text.Y;

            grid.CaptureMouse();

            e.Handled = true;
        }


        private void TextElement_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing)
                return;


            if (!_isDraggingText)
                return;


            if (sender is not Grid grid)
                return;


            if (grid.Tag is not TextElement text)
                return;


            Point currentPosition = e.GetPosition(WhiteBoardCanvas);


            double deltaX = currentPosition.X - _textDragStartMousePosition.X;
            double deltaY = currentPosition.Y - _textDragStartMousePosition.Y;


            double newX = _textDragStartX + deltaX;
            double newY = _textDragStartY + deltaY;

            Canvas.SetLeft(grid, newX);
            Canvas.SetTop(grid, newY);


            text.X = newX;
            text.Y = newY;


            e.Handled = true;
        }


        private void TextElement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingText)
                return;


            if (sender is not Grid grid)
                return;


            _isDraggingText = false;


            if (grid.IsMouseCaptured)
            {
                grid.ReleaseMouseCapture();
            }


            e.Handled = true;
        }

        private void SelectTextElement(Grid? textControl)
        {
            // ========================================================
            // Pfeilauswahl aufheben
            // ========================================================

            if (_selectedArrow != null)
            {
                SelectArrow(null);
            }


            // ========================================================
            // Shape-Auswahl aufheben
            // ========================================================

            foreach (Grid shape in _selectedShapes.ToList())
            {
                SetShapeSelectedVisual(shape, false);

                SetResizeHandlesVisibility(shape, Visibility.Collapsed);
            }

            _selectedShapes.Clear();


            if (_selectedShape != null)
            {
                SetShapeSelectedVisual(_selectedShape, false);

                SetResizeHandlesVisibility(_selectedShape, Visibility.Collapsed);

                _selectedShape = null;
            }


            // ========================================================
            // Alte Textauswahl entfernen
            // ========================================================

            foreach (Grid text in
                     _selectedTextElements.ToList())
            {
                if (text != textControl)
                {
                    SetResizeHandlesVisibility(text, Visibility.Collapsed);
                }
            }

            _selectedTextElements.Clear();


            // ========================================================
            // Neue Auswahl
            // ========================================================

            _selectedTextElement = textControl;


            if (textControl != null)
            {
                _selectedTextElements.Add(textControl);

                SetResizeHandlesVisibility(textControl, Visibility.Visible);

                Panel.SetZIndex(textControl, GetHighestZIndex() + 1);
            }
        }

        private void UpdateTextElementsFromControls()
        {
            foreach (UIElement child in WhiteBoardCanvas.Children)
            {
                if (child is not Grid grid)
                    continue;


                if (grid.Tag is not TextElement text)
                    continue;


                TextBox? textBox = grid.Children.OfType<TextBox>().FirstOrDefault();


                if (textBox == null)
                    continue;


                text.Text = textBox.Text;


                text.X = Canvas.GetLeft(grid);
                text.Y = Canvas.GetTop(grid);

                text.Width = grid.Width;
                text.Height = grid.Height;
            }
        }

        // ============================================================
        // Shapes, Pfeile, Text löschen
        // ============================================================
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;


            if (Keyboard.FocusedElement is TextBox textBox && !textBox.IsReadOnly)
            {
                return;
            }


            DeleteSelectedElement();


            e.Handled = true;
        }

        private void DeleteSelectedElement()
        {
            // ========================================================
            // Shape
            // ========================================================

            if (_selectedShape != null)
            {
                DeleteSelectedShapes();

                return;
            }


            // ========================================================
            // Text
            // ========================================================

            if (_selectedTextElement != null)
            {
                DeleteTextElement(_selectedTextElement);

                return;
            }


            // ========================================================
            // Pfeil
            // ========================================================

            if (_selectedArrow != null)
            {
                DeleteSelectedArrow();

                return;
            }


            StatusText.Text = "Kein Element ausgewählt";
        }

        private void DeleteSelectedShape()
        {
            if (_selectedShape == null)
                return;


            if (_selectedShape.Tag is not ShapeElement shapeModel)
                return;


            Guid shapeId = shapeModel.Id;


            // ========================================================
            // Verbundene Pfeile
            // ========================================================

            var connectedArrows =
                _arrows
                    .Where(arrow =>
                        arrow.SourceId == shapeId ||
                        arrow.TargetId == shapeId)
                    .ToList();


            // ========================================================
            // Pfeildarstellungen entfernen
            // ========================================================

            foreach (var element in
                     WhiteBoardCanvas.Children
                         .OfType<System.Windows.Shapes.Path>()
                         .ToList())
            {
                if (element.Tag is not ArrowElement arrow)
                    continue;


                if (arrow.SourceId == shapeId ||
                    arrow.TargetId == shapeId)
                {
                    WhiteBoardCanvas.Children.Remove(
                        element);
                }
            }


            // ========================================================
            // Pfeile aus Modell entfernen
            // ========================================================

            foreach (var arrow in connectedArrows)
            {
                _arrows.Remove(arrow);
            }


            _selectedArrow = null;


            // ========================================================
            // Shape entfernen
            // ========================================================

            WhiteBoardCanvas.Children.Remove(_selectedShape);


            _selectedShape = null;


            StatusText.Text = "Shape und verbundene Pfeile gelöscht";
        }

        private void DeleteSelectedShapes()
        {
            // ========================================================
            // Keine Shape-Auswahl
            // ========================================================

            if (_selectedShapes.Count == 0)
            {
                if (_selectedShape != null)
                {
                    _selectedShapes.Add(_selectedShape);
                }
                else
                {
                    StatusText.Text = "Kein Shape ausgewählt";
                    return;
                }
            }


            // ========================================================
            // Alle ausgewählten Shapes merken
            // ========================================================

            var shapesToDelete =
                _selectedShapes
                    .Where(shape =>
                        shape.Tag is ShapeElement)
                    .ToList();


            // ========================================================
            // Für jedes Shape:
            // verbundene Pfeile entfernen
            // ========================================================

            foreach (Grid shapeControl in shapesToDelete)
            {
                if (shapeControl.Tag is not ShapeElement shapeModel)
                    continue;


                Guid shapeId =
                    shapeModel.Id;


                // ----------------------------------------------------
                // Pfeile im Canvas entfernen
                // ----------------------------------------------------

                foreach (var element in
                         WhiteBoardCanvas.Children
                             .OfType<System.Windows.Shapes.Path>()
                             .ToList())
                {
                    if (element.Tag is not ArrowElement arrow)
                        continue;


                    if (arrow.SourceId == shapeId ||
                        arrow.TargetId == shapeId)
                    {
                        WhiteBoardCanvas.Children.Remove(
                            element);
                    }
                }


                // ----------------------------------------------------
                // Pfeile aus Modell entfernen
                // ----------------------------------------------------

                var connectedArrows =
                    _arrows
                        .Where(arrow =>
                            arrow.SourceId == shapeId ||
                            arrow.TargetId == shapeId)
                        .ToList();


                foreach (var arrow in connectedArrows)
                {
                    _arrows.Remove(arrow);
                }


                // ----------------------------------------------------
                // Shape entfernen
                // ----------------------------------------------------

                WhiteBoardCanvas.Children.Remove(
                    shapeControl);
            }


            // ========================================================
            // Auswahl zurücksetzen
            // ========================================================

            _selectedShapes.Clear();

            _selectedShape = null;

            _selectedArrow = null;


            StatusText.Text =
                $"{shapesToDelete.Count} Shapes gelöscht";
        }

        private void DeleteSelectedArrow()
        {
            if (_selectedArrow == null)
                return;


            if (_selectedArrow.Tag is not ArrowElement arrow)
                return;


            _arrows.Remove(arrow);


            WhiteBoardCanvas.Children.Remove(_selectedArrow);


            _selectedArrow = null;


            StatusText.Text = "Pfeil gelöscht";
        }

        // ============================================================
        // Shapes, Pfeile, Text mehrfachauswahl
        // ============================================================
        private void AddTextToSelection(Grid textControl)
        {
            if (_selectedTextElements.Contains(textControl))
                return;


            _selectedTextElements.Add(textControl);


            SetResizeHandlesVisibility(textControl, Visibility.Visible);
        }

        private void RemoveTextFromSelection(Grid textControl)
        {
            if (!_selectedTextElements.Remove(textControl))
                return;


            SetResizeHandlesVisibility(textControl, Visibility.Collapsed);


            if (_selectedTextElement == textControl)
            {
                _selectedTextElement = _selectedTextElements.LastOrDefault();
            }
        }

        private bool IsTextSelected(Grid textControl)
        {
            return _selectedTextElements.Contains(textControl);
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