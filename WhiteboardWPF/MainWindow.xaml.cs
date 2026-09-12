namespace WhiteboardWPF
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    using Microsoft.Win32;

    using WhiteboardWPF.Board;
    using WhiteboardWPF.ElementLibrary;
    using WhiteboardWPF.Elements;
    using WhiteboardWPF.Exporter;
    using WhiteboardWPF.Models;
    using WhiteboardWPF.Persistence;
    using WhiteboardWPF.Selection;
    using WhiteboardWPF.ShapeProvider;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // ============================================================
        // Board
        // ============================================================
        private readonly BoardSizeManager _boardSizeManager = new();

        // ============================================================
        // Selektion
        // ============================================================
        private readonly SelectionManager _selectionManager = new();

        // ============================================================
        // Board Laden und Speichern, Export
        // ============================================================
        private readonly BoardPersistenceService _boardPersistenceService = new();
        private readonly BoardDocumentBuilder _boardDocumentBuilder = new();
        private readonly BoardExporter _boardExporter = new();

        // ============================================================
        // Gemeinsame Grundfunktionen für die Erzeugung von Whiteboard-Controls.
        // ============================================================
        private readonly ElementControlFactory _elementControlFactory = new();
        private readonly ShapeVisualFactory _shapeVisualFactory = new();
        private readonly ShapeTextBoxFactory _shapeTextBoxFactory = new();
        private readonly ResizeThumbFactory _resizeThumbFactory = new();
        private readonly TextElementTextBoxFactory _textElementTextBoxFactory = new();
        private readonly SymbolVisualFactory _symbolVisualFactory = new();
        private readonly ResizeCalculator _resizeCalculator = new();

        // ============================================================
        // Verschieben von Shapes und Text-Elemente
        // ============================================================
        private Point _contextMenuPosition;
        private bool _isDragging;
        private Point _dragStartMousePosition;
        private double _dragStartShapeX;
        private double _dragStartShapeY;
        private readonly Dictionary<Grid, Point> _multiDragStartTextPositions = new();

        // ============================================================
        // Text-Elemente
        // ============================================================
        private readonly List<TextElement> _textElements = new();
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

        // ============================================================
        // Mehrfachmarkierung
        // ============================================================
        private readonly Dictionary<Grid, Point> _multiDragStartPositions = new();

        // ============================================================
        // Symbole
        // ============================================================
        private readonly List<SymbolElement> _symbols = new();
        private bool _isDraggingSymbol;
        private Point _symbolDragStartMousePosition;
        private double _symbolDragStartX;
        private double _symbolDragStartY;
        private readonly Dictionary<Grid, Point> _multiDragStartSymbolPositions = new();

        // ============================================================
        // Kopieren
        // ============================================================
        private const double DuplicateOffset = 20;
        private readonly Dictionary<Guid, Guid> _duplicateIdMap = new();

        // ============================================================
        // Shapes und Symbole
        // ============================================================
        private readonly ElementLibrary.ElementLibrary _elementLibrary = new();

        public MainWindow()
        {
            this.InitializeComponent();

            this.Title = "Whiteboard";

            this.InitializeElementLibrary();
            this.InitializeShapeMenu();
            this.InitializeSymbolMenu();
            this.StatusText.Text = "Whiteboard bereit";
        }

        #region Properies
        private Grid? _selectedShape
        {
            get => _selectionManager.SelectedShape;
            set => _selectionManager.SelectedShape = value;
        }

        private List<Grid> _selectedShapes => _selectionManager.SelectedShapes;

        private Grid? _selectedTextElement
        {
            get => _selectionManager.SelectedTextElement;
            set => _selectionManager.SelectedTextElement = value;
        }

        private List<Grid> _selectedTextElements => _selectionManager.SelectedTextElements;

        private Grid? _selectedSymbol
        {
            get => _selectionManager.SelectedSymbol;
            set => _selectionManager.SelectedSymbol = value;
        }

        private List<Grid> _selectedSymbols => _selectionManager.SelectedSymbols;

        private System.Windows.Shapes.Path? _selectedArrow
        {
            get => _selectionManager.SelectedArrow;
            set => _selectionManager.SelectedArrow = value;
        }

        private List<System.Windows.Shapes.Path> _selectedArrows => _selectionManager.SelectedArrows;
        #endregion Properties

        #region Shapes und Symbole Bibliothek
        private void InitializeElementLibrary()
        {
            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Rechteck",
                    "Shape",
                    () => new ShapeElement
                    {
                        ShapeType = ShapeType.Rectangle
                    },
                    ShapeType.Rectangle));

            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Abgerundetes Rechteck",
                    "Shape",
                    () => new ShapeElement
                    {
                        ShapeType = ShapeType.RoundedRectangle
                    },
                    ShapeType.RoundedRectangle));

            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Ellipse",
                    "Shape",
                    () => new ShapeElement
                    {
                        ShapeType = ShapeType.Ellipse
                    },
                    ShapeType.Ellipse));

            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Raute",
                    "Shape",
                    () => new ShapeElement
                    {
                        ShapeType = ShapeType.Diamond
                    },
                    ShapeType.Diamond));

            _elementLibrary.Register(
                new ElementLibraryItem(
                "Sechseck",
                "Shape",
                () => new ShapeElement
                {
                    ShapeType = ShapeType.Hexagon
                },
                ShapeType.Hexagon));

            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Dreieck",
                    "Shape",
                    () => new ShapeElement
                    {
                        ShapeType = ShapeType.Triangle
                    },
                    ShapeType.Triangle));

            _elementLibrary.Register(
                   new ElementLibraryItem(
                       "Info",
                       "Symbol",
                       () => new SymbolElement
                       {
                           SymbolType = "Info"
                       }));

            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Warnung",
                    "Symbol",
                    () => new SymbolElement
                    {
                        SymbolType = "Warning"
                    }));

            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Frage",
                    "Symbol",
                    () => new SymbolElement
                    {
                        SymbolType = "Question"
                    }));

            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Gleich",
                    "Symbol",
                    () => new SymbolElement
                    {
                        SymbolType = "Equals"
                    }));

            _elementLibrary.Register(
                new ElementLibraryItem(
                    "Ungleich",
                    "Symbol",
                    () => new SymbolElement
                    {
                        SymbolType = "NotEquals"
                    }));
        }

        private ShapeElement CreateShapeFromLibrary(ShapeType shapeType)
        {
            ElementLibraryItem? item = _elementLibrary.Items
                .FirstOrDefault(item =>
                    item.Category == "Shape" &&
                    item.ShapeType == shapeType);

            if (item == null)
            {
                throw new InvalidOperationException($"Shape '{shapeType}' ist nicht in der Elementbibliothek registriert.");
            }

            if (item.CreateModel() is not ShapeElement shape)
            {
                throw new InvalidOperationException($"Der Bibliothekseintrag '{item.Name}' erzeugt kein ShapeElement.");
            }

            return shape;
        }

        private SymbolElement CreateSymbolFromLibrary(string symbolType)
        {
            foreach (ElementLibraryItem item in _elementLibrary.Items)
            {
                if (item.Category != "Symbol")
                {
                    continue;
                }

                if (item.CreateModel() is not SymbolElement symbol)
                {
                    continue;
                }

                if (symbol.SymbolType == symbolType)
                {
                    return symbol;
                }
            }

            throw new InvalidOperationException($"Symbol '{symbolType}' ist nicht in der Elementbibliothek registriert.");
        }

        private void InitializeSymbolMenu()
        {
            SymbolMenu.Items.Clear();

            foreach (ElementLibraryItem item in _elementLibrary.Items.Where(item => item.Category == "Symbol"))
            {
                var menuItem = new MenuItem
                {
                    Header = item.Name,
                    Tag = item
                };

                menuItem.Click += LibrarySymbolMenuItem_Click;

                SymbolMenu.Items.Add(menuItem);
            }
        }

        private void LibrarySymbolMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
            {
                return;
            }

            if (menuItem.Tag is not ElementLibraryItem item)
            {
                return;
            }

            if (item.CreateModel() is not SymbolElement symbol)
            {
                return;
            }

            this.AddSymbol(symbol.SymbolType);

            e.Handled = true;
        }

        #endregion Shapes und Symbole Bibliothek

        #region Export als Bild-Datei

        private void ExportBoard_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
                {
                    Title = "Whiteboard exportieren",
                    Filter = "Whiteboard (*.png)|*.png|" + "Alle Dateien (*.*)|*.*",
                    DefaultExt = ".png",
                    AddExtension = true
                };


            if (dialog.ShowDialog() != true)
            {
                return;
            }


            try
            {
                this._boardExporter.ExportAsPng(dialog.FileName,
                    this.WhiteBoardCanvas, 
                    this._selectionManager,
                    this.SetShapeSelectedVisual, 
                    this.SetSymbolSelectedVisual, 
                    this.SetArrowSelectedVisual, 
                    this.SetResizeHandlesVisibility, 
                    this.UpdateLayout);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Das Whiteboard konnte nicht exportiert werden.\n\n" +
                    $"{ex.Message}",
                    "Fehler beim Exportieren",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion Export als Bild-Datei

        private void UpdateBoardSize()
        {
            this._boardSizeManager.Update(WhiteBoardCanvas);
        }

        #region Klick Events 
        /// <summary>
        /// Whiteboard - rechte Maustaste
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WhiteBoardCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this._contextMenuPosition = e.GetPosition(WhiteBoardCanvas);

            this.StatusText.Text = $"Position: X={_contextMenuPosition.X:0}, Y={_contextMenuPosition.Y:0}";
        }

        /// <summary>
        /// Whiteboard - linke Maustaste
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WhiteBoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.SelectShape(null);
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

            this.WhiteBoardCanvas.Children.Add(control);

            this.SelectShape(control);

            this.StatusText.Text = "Shape erstellt";

            this.WhiteBoardContextMenu.IsOpen = false;
        }

        /// <summary>
        /// Contextmenü für Shape
        /// </summary>
        /// <returns></returns>
        private ContextMenu CreateShapeContextMenu()
        {
            var contextMenu = new ContextMenu();


            // Text bearbeiten
            var editTextItem = new MenuItem
            {
                Header = "Text bearbeiten"
            };

            editTextItem.Click += ShapeEditText_Click;

            contextMenu.Items.Add(editTextItem);


            // Trennlinie
            contextMenu.Items.Add(new Separator());


            // Hintergrundfarbe
            var backgroundColorMenuItem = new MenuItem
            {
                Header = "Hintergrundfarbe"
            };

            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Weiß","#FFFFFFFF"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Rot", "Red"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Grün", "Green"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Blau", "Blue"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Gelb", "Yellow"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Hellgelb", "#FFFFF2CC"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Hellgrün", "#FFD9EAD3"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Hellblau", "#FFD9EAF7"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Hellrot", "#FFF4CCCC"));
            backgroundColorMenuItem.Items.Add(CreateBackgroundColorMenuItem("Hellgrau", "#FFE7E6E6"));

            contextMenu.Items.Add(backgroundColorMenuItem);

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
            {
                return;
            }

            if (menuItem.Parent is not ContextMenu contextMenu)
            {
                return;
            }

            if (contextMenu.PlacementTarget is not Grid shape)
            {
                return;
            }

            this.SelectShape(shape);

            this.BeginTextEditing(shape);
        }

        /// <summary>
        /// Text über Doppelklick bearbeiten
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShapeText_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (textBox.Parent is not Grid shape)
            {
                return;
            }

            this.SelectShape(shape);
            this.BeginTextEditing(shape);

            e.Handled = true;
        }

        #endregion Klick Events 

        /// <summary>
        /// Shape-Control erzeugen
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        private Grid CreateShapeControl(ShapeElement shape)
        {
            Grid grid = _elementControlFactory.CreateBaseGrid(shape.Width, shape.Height, shape, shape.X, shape.Y);

            // --------------------------------------------------------
            // Shape
            // --------------------------------------------------------

            var shapeVisual = CreateShapeVisual(shape);

            grid.Children.Add(shapeVisual);


            // --------------------------------------------------------
            // Text
            // --------------------------------------------------------

            var textBox = _shapeTextBoxFactory.Create(shape, ShapeText_MouseDoubleClick, ShapeTextBox_KeyDown, ShapeTextBox_LostFocus);

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
            this.ConfigureShapeMouseEvents(grid);

            // --------------------------------------------------------
            // Resize-Griffe
            // --------------------------------------------------------
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Top, ResizeDirection.TopLeft, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.TopLeft));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Center, VerticalAlignment.Top, ResizeDirection.Top, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Top));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Top, ResizeDirection.TopRight, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.TopRight));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Center, ResizeDirection.Left, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Left));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Center, ResizeDirection.Right, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Right));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Bottom, ResizeDirection.BottomLeft, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.BottomLeft));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Center, VerticalAlignment.Bottom, ResizeDirection.Bottom, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Bottom));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Bottom, ResizeDirection.BottomRight, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.BottomRight));

            return grid;
        }

        private void ConfigureShapeMouseEvents(Grid grid)
        {
            grid.PreviewMouseLeftButtonDown += Shape_PreviewMouseLeftButtonDown;
            grid.PreviewMouseMove += Shape_PreviewMouseMove;
            grid.PreviewMouseLeftButtonUp += Shape_PreviewMouseLeftButtonUp;
        }

        /// <summary>
        /// Textbearbeitung starten
        /// </summary>
        /// <param name="shape"></param>
        private void BeginTextEditing(Grid shape)
        {
            if (shape.Tag is not ShapeElement model)
            {
                return;
            }


            var textBox = this.FindTextBox(shape);

            if (textBox == null)
            {
                return;
            }

            this._editingTextBox = textBox;

            this._textBeforeEditing = model.Text;

            textBox.IsReadOnly = false;
            textBox.Focus();
            textBox.SelectAll();

            this.StatusText.Text = "Text bearbeiten";
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
            {
                return;
            }


            // --------------------------------------------------------
            // Enter = übernehmen
            // --------------------------------------------------------
            if (e.Key == Key.Enter)
            {
                this.FinishTextEditing(textBox);
                e.Handled = true;

                return;
            }


            // --------------------------------------------------------
            // Escape = abbrechen
            // --------------------------------------------------------
            if (e.Key == Key.Escape)
            {
                this.CancelTextEditing(textBox);
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
            {
                return;
            }


            if (this._editingTextBox != textBox)
            {
                return;
            }

            this.FinishTextEditing(textBox);
        }


        /// <summary>
        /// Text übernehmen
        /// </summary>
        /// <param name="textBox"></param>

        private void FinishTextEditing(TextBox textBox)
        {
            if (textBox.Parent is not Grid shape)
            {
                return;
            }


            if (shape.Tag is not ShapeElement model)
            {
                return;
            }

            model.Text = textBox.Text;

            textBox.IsReadOnly = true;

            this._editingTextBox = null;

            this.StatusText.Text = "Text übernommen";
        }


        /// <summary>
        /// Textbearbeitung abbrechen
        /// </summary>
        /// <param name="textBox"></param>

        private void CancelTextEditing(TextBox textBox)
        {
            if (textBox.Parent is not Grid shape)
            {
                return;
            }


            if (shape.Tag is not ShapeElement model)
            {
                return;
            }

            textBox.Text = _textBeforeEditing;

            model.Text = _textBeforeEditing;

            textBox.IsReadOnly = true;
            this._editingTextBox = null;

            this.StatusText.Text = "Textbearbeitung abgebrochen";
        }

        /// <summary>
        /// Resize Cursor
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
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
            {
                return;
            }

            // Pfeil erstellen
            if (_isCreatingArrow)
            {
                CreateArrow(_arrowSourceShape, shape);

                e.Handled = true;

                return;
            }

            // Resize-Griffe
            if (IsResizeThumbSource(e.OriginalSource as DependencyObject))
            {
                return;
            }

            // Doppelklick auf Text
            if (e.ClickCount >= 2 && e.OriginalSource is TextBox)
            {
                return;
            }


            // Normales Verschieben
            if (_editingTextBox != null)
                return;


            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Strg + Klick
                if (IsShapeSelected(shape))
                {
                    this.RemoveShapeFromSelection(shape);

                    this._selectedShape = _selectedShapes.LastOrDefault();
                }
                else
                {
                    this.AddShapeToSelection(shape);

                    this._selectedShape = shape;
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
                if (IsShapeSelected(shape) && (_selectedShapes.Count > 1 || _selectedTextElements.Count > 0))
                {
                    this._selectedShape = shape;
                }
                else
                {
                    this.SelectSingleShape(shape);
                }
            }

            this._isDragging = true;

            this._dragStartMousePosition = e.GetPosition(WhiteBoardCanvas);

            this._dragStartShapeX = Canvas.GetLeft(shape);

            this._dragStartShapeY = Canvas.GetTop(shape);

            if (this._selectedShapes.Count > 0 && this._selectedTextElements.Count > 0)
            {
                this.StartMultiElementDrag();
            }
            else if (this._selectedShapes.Count > 1)
            {
                this.StartMultiElementDrag();
            }

            shape.CaptureMouse();

            e.Handled = true;
        }

        private void CreateArrow(Grid? sourceShape, Grid targetShape)
        {
            if (sourceShape == null)
            {
                this.CancelArrowCreation();
                return;
            }


            if (sourceShape == targetShape)
            {
                this.StatusText.Text = "Quelle und Ziel müssen unterschiedlich sein.";

                return;
            }


            if (sourceShape.Tag is not ShapeElement sourceModel)
            {
                this.CancelArrowCreation();
                return;
            }


            if (targetShape.Tag is not ShapeElement targetModel)
            {
                this.CancelArrowCreation();
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


            if (sourceShape == null || targetShape == null)
            {
                return;
            }


            Point start = GetConnectionPoint(sourceShape, targetShape);
            Point end = GetConnectionPoint(targetShape, sourceShape);

            var path = new System.Windows.Shapes.Path
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


            CreateArrowGeometry(path, start, end);


            path.MouseLeftButtonDown += this.Arrow_MouseLeftButtonDown;


            path.ContextMenu = this.CreateArrowContextMenu();


            path.ContextMenuOpening += this.Arrow_ContextMenuOpening;


            Panel.SetZIndex(path, -1000);


            WhiteBoardCanvas.Children.Add(path);
        }

        private void Arrow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not System.Windows.Shapes.Path arrow)
                return;


            this.SelectArrow(arrow);
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


            Point currentPosition = e.GetPosition(WhiteBoardCanvas);

            double deltaX = currentPosition.X - _dragStartMousePosition.X;
            double deltaY = currentPosition.Y - _dragStartMousePosition.Y;


            // ========================================================
            // Mehrfachauswahl
            // ========================================================

            if (_selectedShapes.Count > 0 && (_selectedTextElements.Count > 0 || _selectedSymbols.Count > 0))
            {
                MoveSelectedElements(deltaX, deltaY);

                e.Handled = true;

                return;
            }

            if (_selectedShapes.Count > 1)
            {
                MoveSelectedShapes(deltaX, deltaY);

                e.Handled = true;

                return;
            }

            // ========================================================
            // Einzelnes Shape
            // ========================================================

            double newX = _dragStartShapeX + deltaX;
            double newY = _dragStartShapeY + deltaY;

            Canvas.SetLeft(shape, newX);
            Canvas.SetTop(shape, newY);

            if (shape.Tag is ShapeElement model)
            {
                model.X = newX;
                model.Y = newY;
            }


            UpdateArrows();
            UpdateBoardSize();

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

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (sender is not Thumb thumb)
                return;

            if (thumb.Parent is not Grid grid)
                return;

            if (thumb.Tag is not ResizeDirection direction)
                return;


            // ========================================================
            // Auswahl
            // ========================================================

            if (grid.Tag is ShapeElement)
            {
                this.SelectShape(grid);
            }
            else if (grid.Tag is TextElement)
            {
                this.SelectTextElement(grid);
            }
            else if (grid.Tag is SymbolElement)
            {
                this._selectedSymbol = grid;

                this.SetSymbolSelectedVisual(grid, true);
                this.SetResizeHandlesVisibility(grid, Visibility.Visible);
            }
            else
            {
                return;
            }


            // ========================================================
            // Resize initialisieren
            // ========================================================

            this.InitializeResize(grid, direction);
        }

        private void InitializeResize(Grid grid, ResizeDirection direction)
        {
            this._isResizing = true;
            this._resizeDirection = direction;
            this._resizeStartMousePosition = Mouse.GetPosition(this.WhiteBoardCanvas);
            this._resizeStartX = Canvas.GetLeft(grid);
            this._resizeStartY = Canvas.GetTop(grid);
            this._resizeStartWidth = grid.ActualWidth;
            this._resizeStartHeight = grid.ActualHeight;
            this.StatusText.Text = "Größe ändern";
        }

        /// <summary>
        /// Resize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this._isResizing == false)
            {
                return;
            }

            if (sender is not Thumb thumb)
            {
                return;
            }

            if (thumb.Parent is not Grid grid)
            {
                return;
            }


            Point currentMousePosition = Mouse.GetPosition(WhiteBoardCanvas);

            // ========================================================
            // Neue Geometrie berechnen
            // ========================================================
            var resizeResult = _resizeCalculator.Calculate(
                _resizeDirection,
                _resizeStartMousePosition,
                currentMousePosition,
                _resizeStartX,
                _resizeStartY,
                _resizeStartWidth,
                _resizeStartHeight,
                MinimumShapeWidth,
                MinimumShapeHeight);

            double newX = resizeResult.X;
            double newY = resizeResult.Y;
            double newWidth = resizeResult.Width;
            double newHeight = resizeResult.Height;


            // ========================================================
            // Control aktualisieren
            // ========================================================
            grid.Width =  newWidth;
            grid.Height = newHeight;

            Canvas.SetLeft(grid, newX);
            Canvas.SetTop(grid, newY);


            // ========================================================
            // Datenmodell aktualisieren
            // ========================================================
            this.UpdateElementSizeAndPosition(grid, newX, newY, newWidth, newHeight);

            // ========================================================
            // Pfeile aktualisieren
            // ========================================================
            this.UpdateArrows();

            this.StatusText.Text = $"Größe: {newWidth:0} x {newHeight:0}";
        }


        private void UpdateElementSizeAndPosition(Grid grid, double x, double y, double width, double height)
        {
            if (grid.Tag is ShapeElement shape)
            {
                shape.X = x;
                shape.Y = y;
                shape.Width = width;
                shape.Height = height;
            }
            else if (grid.Tag is TextElement text)
            {
                text.X = x;
                text.Y = y;
                text.Width = width;
                text.Height = height;
            }
            else if (grid.Tag is SymbolElement symbol)
            {
                symbol.X = x;
                symbol.Y = y;
                symbol.Width = width;
                symbol.Height = height;
            }
        }

        // ============================================================
        // Resize beendet
        // ============================================================

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isResizing = false;


            if (sender is not Thumb thumb)
            {
                return;
            }


            if (thumb.Parent is not Grid grid)
            {
                return;
            }


            this.UpdateResizeCompletedStatus(grid);
        }

        private void UpdateResizeCompletedStatus(Grid grid)
        {
            if (grid.Tag is ShapeElement shape)
            {
                StatusText.Text = $"Shape-Größe: {shape.Width:0} x {shape.Height:0}";

                return;
            }

            if (grid.Tag is TextElement text)
            {
                StatusText.Text = $"Text-Größe: {text.Width:0} x {text.Height:0}";

                return;
            }

            if (grid.Tag is SymbolElement symbol)
            {
                StatusText.Text = $"Symbol-Größe: {symbol.Width:0} x {symbol.Height:0}";
            }
        }

        private void AddShape(ShapeType shapeType)
        {
            ShapeElement shape = this.CreateShapeFromLibrary(shapeType);

            shape.X = _contextMenuPosition.X;
            shape.Y = _contextMenuPosition.Y;

            var control = CreateShapeControl(shape);

            this.WhiteBoardCanvas.Children.Add(control);

            this.SelectShape(control);

            this.StatusText.Text = $"{GetShapeName(shapeType)} erstellt";

            this.WhiteBoardContextMenu.IsOpen = false;

            this.UpdateBoardSize();
        }

        private string GetShapeName(ShapeType shapeType)
        {
            return ShapeDefinitionProvider.GetDefinition(shapeType) ?.Name ?? "Shape";
        }

        private FrameworkElement CreateShapeVisual(ShapeElement shape)
        {
            return _shapeVisualFactory.Create(shape, GetShapeBackgroundBrush(shape));
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

        /// <summary>
        /// Auswahl
        /// </summary>
        /// <param name="shape"></param>
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

            this.ClearBoard();

            this.StatusText.Text = "Neues Whiteboard erstellt";
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
            int deletedShapes = 0;
            int deletedTexts = 0;

            // ========================================================
            // Symbole löschen
            // ========================================================

            if (_selectedSymbols.Count > 0)
            {
                foreach (Grid symbolControl in
                         _selectedSymbols.ToList())
                {
                    DeleteSymbol(
                        symbolControl);
                }

                _selectedSymbols.Clear();

                _selectedSymbol = null;
            }

            // ========================================================
            // Mehrfach ausgewählte Shapes
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


                    deletedShapes++;
                }


                _selectedShapes.Clear();

                _selectedShape = null;

                _selectedArrow = null;
            }


            // ========================================================
            // Einzelnes Shape
            // ========================================================

            else if (_selectedShape != null)
            {
                if (_selectedShape.Tag is ShapeElement shapeModel)
                {
                    Guid shapeId =
                        shapeModel.Id;


                    var connectedArrows =
                        _arrows
                            .Where(arrow =>
                                arrow.SourceId == shapeId ||
                                arrow.TargetId == shapeId)
                            .ToList();


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


                    foreach (var arrow in connectedArrows)
                    {
                        _arrows.Remove(arrow);
                    }


                    WhiteBoardCanvas.Children.Remove(
                        _selectedShape);


                    _selectedShape = null;

                    _selectedArrow = null;

                    deletedShapes++;
                }
            }


            // ========================================================
            // Mehrfach ausgewählte Text-Elemente
            // ========================================================

            if (this._selectedTextElements.Count > 0)
            {
                var textsToDelete = this._selectedTextElements.ToList();


                foreach (Grid textControl in textsToDelete)
                {
                    this.DeleteTextElement(textControl);

                    deletedTexts++;
                }


                _selectedTextElements.Clear();

                _selectedTextElement = null;
            }


            // ========================================================
            // Einzelnes Text-Element
            // ========================================================

            else if (this._selectedTextElement != null)
            {
                this.DeleteTextElement(this._selectedTextElement);

                deletedTexts++;
            }


            // ========================================================
            // Ergebnis
            // ========================================================

            if (deletedShapes > 0 || deletedTexts > 0)
            {
                this.StatusText.Text = $"{deletedShapes} Shape(s), {deletedTexts} Text(e) gelöscht";

                return;
            }


            this.StatusText.Text = "Kein Element ausgewählt";
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
            this.ClearBoard();

            this.StatusText.Text = "Whiteboard zurückgesetzt";
        }


        private void ClearBoard()
        {
            _isDragging = false;
            _isDraggingText = false;
            _isDraggingSymbol = false;
            _isResizing = false;
            _editingTextBox = null;

            _isCreatingArrow = false;
            _arrowSourceShape = null;

            _selectedShape = null;
            _selectedTextElement = null;
            _selectedSymbol = null;
            _selectedArrow = null;

            _selectedShapes.Clear();
            _selectedTextElements.Clear();
            _selectedSymbols.Clear();
            _selectedArrows.Clear();

            _multiDragStartPositions.Clear();
            _multiDragStartTextPositions.Clear();
            _multiDragStartSymbolPositions.Clear();

            _arrows.Clear();
            _symbols.Clear();

            WhiteBoardCanvas.Children.Clear();

            UpdateBoardSize();
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
            this.UpdateTextElementsFromControls();

            var document = this._boardDocumentBuilder.Build(this.GetShapeModels(), this._textElements, this._arrows, this._symbols);

            this._boardPersistenceService.Save(fileName, document);
            this.StatusText.Text = $"Board gespeichert: {fileName}";
        }

        private List<ShapeElement> GetShapeModels()
        {
            return WhiteBoardCanvas.Children.OfType<Grid>()
                .Where(grid => grid.Tag is ShapeElement)
                .Select(grid => (ShapeElement)grid.Tag).ToList();
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
                this.LoadBoard(dialog.FileName);
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
            WhiteBoardDocument document = this._boardPersistenceService.Load(fileName);

            this.ClearBoard();


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
            // Symbole
            // ========================================================

            foreach (SymbolElement symbol in document.Symbols)
            {
                AddLoadedSymbol(symbol);
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

            this.SelectShape(null);

            this.SelectTextElement(null);

            this.SelectArrow(null);
            this.UpdateBoardSize();

            this.StatusText.Text = $"Board geladen: {fileName}";
        }

        private void AddLoadedShape(ShapeElement shape)
        {
            var control = CreateShapeControl(shape);

            WhiteBoardCanvas.Children.Add(control);
        }

        private void AddLoadedSymbol(SymbolElement symbol)
        {
            // ========================================================
            // Datenmodell übernehmen
            // ========================================================

            _symbols.Add(symbol);

            // ========================================================
            // Control erzeugen
            // ========================================================

            var control = CreateSymbolControl(symbol);

            WhiteBoardCanvas.Children.Add(control);
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


            this._selectedShapes.Add(shape);

            this.SetShapeSelectedVisual(shape, true);
            this.SetResizeHandlesVisibility(shape, Visibility.Visible);
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

        private void MoveSelectedShapes(double deltaX, double deltaY)
        {
            foreach (Grid shape in _selectedShapes.ToList())
            {
                if (!_multiDragStartPositions.TryGetValue(shape, out Point start))
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

            this.UpdateArrows();
            this.UpdateBoardSize();
            this.StatusText.Text = $"{_selectedShapes.Count} Shapes verschoben";
        }

        // ============================================================
        // Shape Contextmenu
        // ============================================================
        private void InitializeShapeMenu()
        {
            ShapeMenu.Items.Clear();

            foreach (ElementLibraryItem item in _elementLibrary.Items
                         .Where(item =>
                             item.Category == "Shape" &&
                             item.ShapeType.HasValue))
            {
#pragma warning disable CS8629 // Ein Werttyp, der NULL zulässt, kann NULL sein.
                var menuItem = new MenuItem
                {
                    Header = item.Name,
                    Tag = item.ShapeType.Value
                };
#pragma warning restore CS8629 // Ein Werttyp, der NULL zulässt, kann NULL sein.

                menuItem.Click += LibraryShapeMenuItem_Click;

                ShapeMenu.Items.Add(menuItem);
            }
        }

        private void LibraryShapeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            if (menuItem.Tag is not ShapeType shapeType)
                return;

            AddShape(shapeType);

            e.Handled = true;
        }

        private void StartMultiElementDrag()
        {
            this._multiDragStartPositions.Clear();
            this._multiDragStartTextPositions.Clear();
            this._multiDragStartSymbolPositions.Clear();

            // Shapes
            foreach (Grid shape in _selectedShapes)
            {
                _multiDragStartPositions[shape] = new Point(Canvas.GetLeft(shape), Canvas.GetTop(shape));
            }

            // Text-Elemente
            foreach (Grid text in _selectedTextElements)
            {
                _multiDragStartTextPositions[text] = new Point(Canvas.GetLeft(text), Canvas.GetTop(text));
            }

            // Symbole
            foreach (Grid symbol in _selectedSymbols)
            {
                _multiDragStartSymbolPositions[symbol] = new Point(Canvas.GetLeft(symbol), Canvas.GetTop(symbol));
            }
        }

        private void MoveSelectedElements(double deltaX, double deltaY)
        {
            // Shapes
            foreach (Grid shape in _selectedShapes.ToList())
            {
                if (!_multiDragStartPositions.TryGetValue(shape, out Point start))
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

            // Text-Elemente
            foreach (Grid textControl in _selectedTextElements.ToList())
            {
                if (!_multiDragStartTextPositions.TryGetValue(textControl, out Point start))
                {
                    continue;
                }


                double newX = start.X + deltaX;
                double newY = start.Y + deltaY;

                Canvas.SetLeft(textControl, newX);
                Canvas.SetTop(textControl, newY);

                if (textControl.Tag is TextElement model)
                {
                    model.X = newX;
                    model.Y = newY;
                }
            }


            // Symbole
            foreach (Grid symbolControl in _selectedSymbols.ToList())
            {
                if (!_multiDragStartSymbolPositions.TryGetValue(symbolControl, out Point start))
                {
                    continue;
                }

                double newX = start.X + deltaX;
                double newY = start.Y + deltaY;

                Canvas.SetLeft(symbolControl, newX);
                Canvas.SetTop(symbolControl, newY);

                if (symbolControl.Tag is SymbolElement symbol)
                {
                    symbol.X = newX;
                    symbol.Y = newY;
                }
            }

            // Pfeile aktualisieren
            this.UpdateArrows();
            this.UpdateBoardSize();

            this.StatusText.Text = $"{_selectedShapes.Count} Shapes, {_selectedTextElements.Count} Texte und {_selectedSymbols.Count} Symbole verschoben";
        }

        private void AddShapeFromMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;


            if (item.Tag is not ShapeType shapeType)
                return;

            AddShape(shapeType);
        }

        private MenuItem CreateBackgroundColorMenuItem(string name, string color)
        {
            var menuItem = new MenuItem
            {
                Header = name,
                Tag = color
            };


            menuItem.Click += ShapeBackgroundColor_Click;


            return menuItem;
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

            this.WhiteBoardCanvas.Children.Add(control);
            this.WhiteBoardContextMenu.IsOpen = false;
            this.UpdateBoardSize();
            this.StatusText.Text = "Text erstellt";
        }

        private Grid CreateTextControl(TextElement text)
        {
            Grid grid = _elementControlFactory.CreateBaseGrid(text.Width, text.Height, text, text.X, text.Y);

            var textBox = _textElementTextBoxFactory.Create(text, TextElement_KeyDown, TextElement_LostFocus);

            grid.Children.Add(textBox);
            grid.ContextMenu = CreateTextElementContextMenu();

            // --------------------------------------------------------
            // Verschieben
            // --------------------------------------------------------
            this.ConfigureTextElementMouseEvents(grid);

            // --------------------------------------------------------
            // Resize-Griffe
            // --------------------------------------------------------
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Top, ResizeDirection.TopLeft, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.TopLeft));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Center, VerticalAlignment.Top, ResizeDirection.Top, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Top));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Top, ResizeDirection.TopRight, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.TopRight));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Center, ResizeDirection.Left, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Left));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Center, ResizeDirection.Right, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Right));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Bottom, ResizeDirection.BottomLeft, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.BottomLeft));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Center, VerticalAlignment.Bottom, ResizeDirection.Bottom, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Bottom));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Bottom, ResizeDirection.BottomRight, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.BottomRight));

            Canvas.SetLeft(grid, text.X);
            Canvas.SetTop(grid, text.Y);

            return grid;
        }

        private void ConfigureTextElementMouseEvents(Grid grid)
        {
            grid.PreviewMouseLeftButtonDown += TextElement_PreviewMouseLeftButtonDown;
            grid.PreviewMouseMove += TextElement_PreviewMouseMove;
            grid.PreviewMouseLeftButtonUp += TextElement_PreviewMouseLeftButtonUp;
        }

        private ContextMenu CreateTextElementContextMenu()
        {
            var contextMenu = new ContextMenu();

            var deleteMenuItem = new MenuItem
            {
                Header = "Löschen"
            };

            deleteMenuItem.Click += TextElement_DeleteClick;

            contextMenu.Items.Add(deleteMenuItem);

            return contextMenu;
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

                    textBox.Cursor =
                        Cursors.IBeam;

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
            // Normale Auswahl
            // ========================================================

            // Wenn dieses Text-Element bereits Teil einer
            // Mehrfachauswahl ist, Auswahl beibehalten.
            bool isPartOfMultipleSelection =
                IsTextSelected(grid) &&
                (
                    _selectedTextElements.Count > 1 ||
                    _selectedShapes.Count > 0
                );


            if (!isPartOfMultipleSelection)
            {
                SelectTextElement(grid);
            }
            else
            {
                _selectedTextElement =
                    grid;
            }


            // ========================================================
            // Verschieben starten
            // ========================================================

            _isDraggingText = true;

            _textDragStartMousePosition =
                e.GetPosition(
                    WhiteBoardCanvas);

            _textDragStartX =
                text.X;

            _textDragStartY =
                text.Y;


            // ========================================================
            // Mehrfach-Drag
            // ========================================================

            if (_selectedTextElements.Count > 1 ||
                (_selectedShapes.Count > 0 &&
                 _selectedTextElements.Count > 0))
            {
                StartMultiElementDrag();
            }


            grid.CaptureMouse();

            e.Handled = true;
        }


        private void TextElement_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // ========================================================
            // Resize hat Vorrang
            // ========================================================

            if (_isResizing)
                return;


            if (!_isDraggingText)
                return;


            if (sender is not Grid grid)
                return;


            if (grid.Tag is not TextElement text)
                return;


            // ========================================================
            // Shape + Text gemeinsam verschieben
            // ========================================================

            if ((_selectedShapes.Count > 0 || _selectedSymbols.Count > 0) && _selectedTextElements.Count > 0)
            {
                Point currentPosition = e.GetPosition(WhiteBoardCanvas);

                double deltaX = currentPosition.X - _textDragStartMousePosition.X;
                double deltaY = currentPosition.Y - _textDragStartMousePosition.Y;

                MoveSelectedElements(deltaX, deltaY);

                e.Handled = true;

                return;
            }

            // ========================================================
            // Mehrere Text-Elemente gemeinsam verschieben
            // ========================================================

            if (_selectedTextElements.Count > 1)
            {
                Point currentPosition = e.GetPosition(WhiteBoardCanvas);

                double deltaX = currentPosition.X - _textDragStartMousePosition.X;
                double deltaY = currentPosition.Y - _textDragStartMousePosition.Y;

                MoveSelectedElements(deltaX, deltaY);

                e.Handled = true;

                return;
            }


            // ========================================================
            // Einzelnes Text-Element verschieben
            // ========================================================

            Point singleCurrentPosition = e.GetPosition(WhiteBoardCanvas);


            double singleDeltaX = singleCurrentPosition.X - _textDragStartMousePosition.X;
            double singleDeltaY = singleCurrentPosition.Y - _textDragStartMousePosition.Y;

            double newX = _textDragStartX + singleDeltaX;
            double newY = _textDragStartY + singleDeltaY;

            Canvas.SetLeft(grid, newX);
            Canvas.SetTop(grid, newY);

            text.X = newX;
            text.Y = newY;

            this.StatusText.Text = $"Text: X={newX:0}, Y={newY:0}";
            this.UpdateBoardSize();

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
            // ========================================================
            // ESC -> Auswahl aufheben
            // ========================================================

            if (e.Key == Key.Escape)
            {
                if (Keyboard.FocusedElement is TextBox textBoxESC && !textBoxESC.IsReadOnly)
                {
                    textBoxESC.IsReadOnly = true;

                    textBoxESC.Cursor =  Cursors.Arrow;

                    textBoxESC.Text = textBoxESC.Text;

                    textBoxESC.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

                    e.Handled = true;

                    return;
                }


                this.ClearAllSelections();

                e.Handled = true;

                return;
            }

            // ========================================================
            // Strg+A
            // ========================================================

            if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) ==  ModifierKeys.Control)
            {
                this.SelectAllElements();

                e.Handled = true;

                return;
            }

            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                this.DuplicateSelectedElements();

                e.Handled = true;

                return;
            }

            // ========================================================
            // Delete
            // ========================================================

            if (e.Key != Key.Delete)
                return;


            if (Keyboard.FocusedElement is TextBox textBox && !textBox.IsReadOnly)
            {
                return;
            }


            this.DeleteSelectedElement();

            e.Handled = true;
        }

        private void DeleteSelectedElement()
        {
            // ========================================================
            // Shape / Text / Symbol
            // ========================================================

            if (_selectedShapes.Count > 0 ||
                _selectedShape != null ||
                _selectedTextElements.Count > 0 ||
                _selectedTextElement != null ||
                _selectedSymbols.Count > 0 ||
                _selectedSymbol != null)
            {
                Delete_Click(null!, null!);

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

        private void ClearAllSelections()
        {
            // ========================================================
            // Shapes
            // ========================================================

            foreach (Grid shape in _selectedShapes.ToList())
            {
                SetShapeSelectedVisual(shape, false);
                SetResizeHandlesVisibility(shape, Visibility.Collapsed);
            }

            _selectedShapes.Clear();


            // ========================================================
            // Einzelnes Shape
            // ========================================================

            if (_selectedShape != null)
            {
                SetShapeSelectedVisual(
                    _selectedShape,
                    false);

                SetResizeHandlesVisibility(
                    _selectedShape,
                    Visibility.Collapsed);

                _selectedShape = null;
            }


            // ========================================================
            // Text-Elemente
            // ========================================================

            foreach (Grid text in _selectedTextElements.ToList())
            {
                SetResizeHandlesVisibility(
                    text,
                    Visibility.Collapsed);
            }

            _selectedTextElements.Clear();

            _selectedTextElement = null;

            // ========================================================
            // Symbole
            // ========================================================

            foreach (Grid symbol in
                     _selectedSymbols.ToList())
            {
                SetSymbolSelectedVisual(
                    symbol,
                    false);

                SetResizeHandlesVisibility(
                    symbol,
                    Visibility.Collapsed);
            }


            _selectedSymbols.Clear();

            _selectedSymbol = null;

            // ========================================================
            // Pfeil Einzel- und Mehrfachauswahl
            // ========================================================

            foreach (System.Windows.Shapes.Path arrow in _selectedArrows.ToList())
            {
                SetArrowSelectedVisual(arrow, false);
            }

            _selectedArrows.Clear();

            // ========================================================
            // Drag-Zustände zurücksetzen
            // ========================================================

            _isDragging = false;
            _isDraggingText = false;
            _isResizing = false;

            _multiDragStartPositions.Clear();
            _multiDragStartTextPositions.Clear();

            StatusText.Text = "Auswahl aufgehoben";
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

        private void SelectAllElements()
        {
            // ========================================================
            // Bestehende Auswahl vollständig entfernen
            // ========================================================

            ClearAllSelections();


            // ========================================================
            // Shapes auswählen
            // ========================================================

            foreach (Grid shape in WhiteBoardCanvas.Children
                         .OfType<Grid>()
                         .Where(grid =>
                             grid.Tag is ShapeElement)
                         .ToList())
            {
                AddShapeToSelection(shape);
            }


            // ========================================================
            // Text-Elemente auswählen
            // ========================================================

            foreach (Grid text in WhiteBoardCanvas.Children.OfType<Grid>()
                .Where(grid => grid.Tag is TextElement).ToList())
            {
                AddTextToSelection(text);
            }

            // ========================================================
            // Symbole auswählen
            // ========================================================
            foreach (Grid symbol in
                     WhiteBoardCanvas.Children
                         .OfType<Grid>()
                         .Where(grid =>
                             grid.Tag is SymbolElement)
                         .ToList())
            {
                AddSymbolToSelection(symbol);
            }

            // ========================================================
            // Pfeile auswählen
            // ========================================================

            foreach (System.Windows.Shapes.Path path in WhiteBoardCanvas.Children
                         .OfType<System.Windows.Shapes.Path>().ToList())
            {
                if (path.Tag is ArrowElement)
                {
                    AddArrowToSelection(path);
                }
            }

            StatusText.Text = $"{_selectedShapes.Count} Shape(s), " + $"{_selectedTextElements.Count} Text(e) " + "und Pfeile ausgewählt";
        }

        private void AddArrowToSelection(System.Windows.Shapes.Path arrow)
        {
            if (_selectedArrows.Contains(arrow))
                return;


            _selectedArrows.Add(arrow);


            SetArrowSelectedVisual(arrow, true);
        }

        // ============================================================
        // Shape Color
        // ============================================================

        private Brush GetShapeBackgroundBrush(ShapeElement shape)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(shape.BackgroundColor);

                return new SolidColorBrush(color);
            }
            catch
            {
                return Brushes.White;
            }
        }

        private void ShapeBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            if (menuItem.Tag is not string colorString)
                return;


            // Übergeordnetes Menü "Hintergrundfarbe"
            if (menuItem.Parent is not MenuItem backgroundMenu)
                return;

            // ContextMenu ermitteln
            if (backgroundMenu.Parent is not ContextMenu contextMenu)
                return;

            // Shape-Control ermitteln

            if (contextMenu.PlacementTarget is not Grid shapeControl)
                return;

            // Shape-Modell ermitteln

            if (shapeControl.Tag is not ShapeElement shape)
                return;

            // Farbe setzen
            SetShapeBackgroundColor(shapeControl, shape, colorString);


            contextMenu.IsOpen = false;

            e.Handled = true;
        }

        private void SetShapeBackgroundColor(Grid shapeControl, ShapeElement shape, string colorString)
        {
            // Farbe prüfen
            Color color;

            try
            {
                color = (Color)ColorConverter.ConvertFromString(colorString);
            }
            catch
            {
                return;
            }


            // Datenmodell aktualisieren
            shape.BackgroundColor = colorString;

            // Shape-Visual suchen
            FrameworkElement? shapeVisual =
                shapeControl.Children
                    .OfType<FrameworkElement>()
                    .FirstOrDefault(element =>
                        element is Border ||
                        element is System.Windows.Shapes.Shape);


            if (shapeVisual == null)
                return;

            // Brush erzeugen
            Brush brush = new SolidColorBrush(color);


            // Hintergrund/Füllung setzen
            switch (shapeVisual)
            {
                case Border border:

                    border.Background = brush;

                    break;


                case System.Windows.Shapes.Shape visual:

                    visual.Fill = brush;

                    break;
            }


            StatusText.Text = $"Hintergrundfarbe geändert: {colorString}";
        }

        private Grid CreateSymbolControl(SymbolElement symbol)
        {
            Grid grid = _elementControlFactory.CreateBaseGrid(symbol.Width, symbol.Height, symbol, symbol.X, symbol.Y);

            // ========================================================
            // Symbol
            // ========================================================

            var image = new Image
            {
                Source = _symbolVisualFactory.Create(symbol.SymbolType),
                Stretch = Stretch.Fill,
                IsHitTestVisible = true
            };

            grid.Children.Add(image);

            // ========================================================
            // Position
            // ========================================================

            Canvas.SetLeft(grid, symbol.X);
            Canvas.SetTop(grid, symbol.Y);

            // ========================================================
            // Verschieben
            // ========================================================

            this.ConfigureSymbolMouseEvents(grid);

            // --------------------------------------------------------
            // Resize-Griffe
            // --------------------------------------------------------
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Top, ResizeDirection.TopLeft, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.TopLeft));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Center, VerticalAlignment.Top, ResizeDirection.Top, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Top));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Top, ResizeDirection.TopRight, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.TopRight));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Center, ResizeDirection.Left, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Left));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Center, ResizeDirection.Right, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Right));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Left, VerticalAlignment.Bottom, ResizeDirection.BottomLeft, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.BottomLeft));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Center, VerticalAlignment.Bottom, ResizeDirection.Bottom, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.Bottom));
            _resizeThumbFactory.Add(grid, HorizontalAlignment.Right, VerticalAlignment.Bottom, ResizeDirection.BottomRight, ResizeThumb_DragStarted, ResizeThumb_DragDelta, ResizeThumb_DragCompleted, GetResizeCursor(ResizeDirection.BottomRight));

            // ========================================================
            // Contextmenü
            // ========================================================

            grid.ContextMenu = CreateSymbolContextMenu();

            return grid;
        }

        private void ConfigureSymbolMouseEvents(Grid grid)
        {
            grid.PreviewMouseLeftButtonDown += Symbol_PreviewMouseLeftButtonDown;
            grid.PreviewMouseMove += Symbol_PreviewMouseMove;
            grid.PreviewMouseLeftButtonUp += Symbol_PreviewMouseLeftButtonUp;
        }

        private ContextMenu CreateSymbolContextMenu()
        {
            var contextMenu = new ContextMenu();

            var deleteItem = new MenuItem
                {
                    Header = "Löschen"
                };


            deleteItem.Click += SymbolDelete_Click;

            contextMenu.Items.Add(deleteItem);

            return contextMenu;
        }

        private void SymbolDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;


            if (menuItem.Parent is not ContextMenu contextMenu)
                return;


            if (contextMenu.PlacementTarget is not Grid grid)
                return;


            DeleteSymbol(grid);
        }

        private void DeleteSymbol(Grid symbolControl)
        {
            if (symbolControl.Tag is not SymbolElement symbol)
                return;


            // ========================================================
            // Aus Datenmodell entfernen
            // ========================================================

            _symbols.Remove(symbol);


            // ========================================================
            // Auswahl entfernen
            // ========================================================

            if (_selectedSymbol == symbolControl)
            {
                _selectedSymbol = null;
            }

            SetSymbolSelectedVisual(symbolControl, false);
            SetResizeHandlesVisibility(symbolControl, Visibility.Collapsed);

            // ========================================================
            // Control vom Canvas entfernen
            // ========================================================

            WhiteBoardCanvas.Children.Remove(symbolControl);

            if (_selectedSymbols.Count <= 1)
            {
                StatusText.Text = "Symbol gelöscht";
            }
        }

        private void AddSymbol(string symbolType)
        {
            SymbolElement symbol = CreateSymbolFromLibrary(symbolType);

            symbol.X = _contextMenuPosition.X;
            symbol.Y = _contextMenuPosition.Y;

            symbol.Width = 80;
            symbol.Height = 80;

            _symbols.Add(symbol);

            var control = CreateSymbolControl(symbol);

            this.WhiteBoardCanvas.Children.Add(control);

            this.StatusText.Text = "Symbol erstellt";

            this.UpdateBoardSize();

            this.WhiteBoardContextMenu.IsOpen = false;
        }

        private void Symbol_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Grid grid)
                return;


            if (grid.Tag is not SymbolElement symbol)
                return;


            // ========================================================
            // Resize-Griff
            // ========================================================

            if (IsResizeThumbSource(e.OriginalSource as DependencyObject))
            {
                return;
            }


            // ========================================================
            // Doppelklick / normales Symbol
            // ========================================================

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // ====================================================
                // Strg + Klick
                // ====================================================

                if (IsSymbolSelected(grid))
                {
                    RemoveSymbolFromSelection(grid);

                    _selectedSymbol = _selectedSymbols.LastOrDefault();
                }
                else
                {
                    AddSymbolToSelection(grid);

                    _selectedSymbol = grid;
                }
            }
            else
            {
                // ====================================================
                // Normaler Klick
                // ====================================================

                bool keepMultipleSelection = IsSymbolSelected(grid) &&
                    (
                        _selectedSymbols.Count > 1 || _selectedShapes.Count > 0 || _selectedTextElements.Count > 0
                    );


                if (keepMultipleSelection)
                {
                    _selectedSymbol = grid;
                }
                else
                {
                    this.ClearAllSelections();

                    this.AddSymbolToSelection(grid);

                    this._selectedSymbol = grid;
                }
            }


            // ========================================================
            // Drag vorbereiten
            // ========================================================

            _isDraggingSymbol = true;


            _symbolDragStartMousePosition =
                e.GetPosition(
                    WhiteBoardCanvas);


            _symbolDragStartX =
                symbol.X;

            _symbolDragStartY =
                symbol.Y;


            // ========================================================
            // Mehrfach-Drag vorbereiten
            // ========================================================

            if (_selectedSymbols.Count > 0 &&
                (
                    _selectedShapes.Count > 0 ||
                    _selectedTextElements.Count > 0 ||
                    _selectedSymbols.Count > 1
                ))
            {
                StartMultiElementDrag();
            }


            grid.CaptureMouse();

            e.Handled = true;
        }


        private void Symbol_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingSymbol)
                return;


            if (sender is not Grid grid)
                return;


            if (grid.Tag is not SymbolElement symbol)
                return;


            Point currentPosition = e.GetPosition(WhiteBoardCanvas);

            double deltaX = currentPosition.X - _symbolDragStartMousePosition.X;
            double deltaY = currentPosition.Y - _symbolDragStartMousePosition.Y;

            // ========================================================
            // Mehrfachauswahl
            // ========================================================

            if (_selectedSymbols.Count > 1 ||
                (_selectedSymbols.Count > 0 &&
                 _selectedShapes.Count > 0) ||
                (_selectedSymbols.Count > 0 &&
                 _selectedTextElements.Count > 0))
            {
                MoveSelectedElements(deltaX,  deltaY);

                e.Handled = true;

                return;
            }


            // ========================================================
            // Einzelnes Symbol
            // ========================================================

            double newX = _symbolDragStartX + deltaX;
            double newY = _symbolDragStartY + deltaY;

            Canvas.SetLeft( grid, newX);
            Canvas.SetTop(grid, newY);

            symbol.X = newX;
            symbol.Y = newY;

            StatusText.Text = $"Symbol: X={newX:0}, Y={newY:0}";
            UpdateBoardSize();

            e.Handled = true;
        }

        private void SetSymbolSelectedVisual(Grid symbol, bool selected)
        {
            if (symbol.Children
                .OfType<Image>()
                .FirstOrDefault() is not Image image)
            {
                return;
            }


            if (selected)
            {
                image.Opacity = 0.75;
            }
            else
            {
                image.Opacity = 1.0;
            }
        }

        private void Symbol_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Grid grid)
                return;


            if (!_isDraggingSymbol)
                return;


            _isDraggingSymbol = false;


            if (grid.IsMouseCaptured)
            {
                grid.ReleaseMouseCapture();
            }


            if (grid.Tag is SymbolElement symbol)
            {
                StatusText.Text = $"Symbol positioniert: " + $"X={symbol.X:0}, Y={symbol.Y:0}";
            }


            e.Handled = true;
        }

        private void AddSymbolToSelection(Grid symbol)
        {
            if (_selectedSymbols.Contains(symbol))
                return;


            _selectedSymbols.Add(symbol);


            SetSymbolSelectedVisual(
                symbol,
                true);


            SetResizeHandlesVisibility(
                symbol,
                Visibility.Visible);
        }

        private void RemoveSymbolFromSelection(Grid symbol)
        {
            if (!_selectedSymbols.Remove(symbol))
                return;


            SetSymbolSelectedVisual(
                symbol,
                false);


            SetResizeHandlesVisibility(
                symbol,
                Visibility.Collapsed);


            if (_selectedSymbol == symbol)
            {
                _selectedSymbol =
                    _selectedSymbols.LastOrDefault();
            }
        }

        private bool IsSymbolSelected(Grid symbol)
        {
            return _selectedSymbols.Contains(symbol);
        }

        // ============================================================
        // Kopieren
        // ============================================================
        private void DuplicateSelectedElements()
        {
            if (_selectedShapes.Count == 0 && _selectedTextElements.Count == 0 && _selectedSymbols.Count == 0)
            {
                this.StatusText.Text = "Keine Elemente ausgewählt";
                return;
            }

            // Shapes duplizieren
            List<Grid> shapeDuplicates = DuplicateSelectedShapes();

            // Texte duplizieren
            List<Grid> textDuplicates = DuplicateSelectedTextElements();

            // Symbole duplizieren
            List<Grid> symbolDuplicates = DuplicateSelectedSymbols();

            // Pfeile zwischen duplizierten Shapes duplizieren
            this.DuplicateSelectedArrows();

            // Neue Elemente auswählen
            this.SelectDuplicatedElements(shapeDuplicates, textDuplicates, symbolDuplicates);

            this.UpdateBoardSize();
            this.StatusText.Text = $"{shapeDuplicates.Count} Shapes, {textDuplicates.Count} Texte und {symbolDuplicates.Count} Symbole dupliziert";
        }


        private Grid? DuplicateShape(Grid source)
        {
            if (source.Tag is not ShapeElement original)
                return null;


            // ========================================================
            // Kopie des Datenmodells erzeugen
            // ========================================================

            var duplicate = new ShapeElement
            {
                Id = Guid.CreateVersion7(),

                ShapeType = original.ShapeType,

                X = original.X + DuplicateOffset,
                Y = original.Y + DuplicateOffset,

                Width = original.Width,
                Height = original.Height,

                Text = original.Text,

                BackgroundColor = original.BackgroundColor
            };


            // ========================================================
            // Control erzeugen
            // ========================================================

            var control = CreateShapeControl(duplicate);


            WhiteBoardCanvas.Children.Add(control);


            return control;
        }

        private List<Grid> DuplicateSelectedShapes()
        {
            var duplicates = new List<Grid>();


            _duplicateIdMap.Clear();


            foreach (Grid source in this._selectedShapes.ToList())
            {
                if (source.Tag is not ShapeElement original)
                {
                    continue;
                }


                Grid? duplicate = this.DuplicateShape(source);

                if (duplicate == null)
                {
                    continue;
                }

                if (duplicate.Tag is ShapeElement copy)
                {
                    _duplicateIdMap[original.Id] = copy.Id;
                }

                duplicates.Add(duplicate);
            }

            return duplicates;
        }

        private Grid? DuplicateTextElement(Grid source)
        {
            if (source.Tag is not TextElement original)
            {
                return null;
            }

            var duplicate = new TextElement
            {
                Id = Guid.CreateVersion7(),
                X = original.X + DuplicateOffset,
                Y = original.Y + DuplicateOffset,
                Width = original.Width,
                Height = original.Height,
                Text = original.Text,
                FontSize = original.FontSize
            };

            var control = CreateTextControl(duplicate);

            WhiteBoardCanvas.Children.Add(control);

            return control;
        }

        private List<Grid> DuplicateSelectedTextElements()
        {
            var duplicates = new List<Grid>();

            foreach (Grid source in _selectedTextElements.ToList())
            {
                Grid? duplicate = DuplicateTextElement(source);

                if (duplicate != null)
                {
                    duplicates.Add(duplicate);
                }
            }

            return duplicates;
        }

        private Grid? DuplicateSymbol(Grid source)
        {
            if (source.Tag is not SymbolElement original)
            {
                return null;
            }

            var duplicate = new SymbolElement
            {
                Id = Guid.CreateVersion7(),
                X = original.X + DuplicateOffset,
                Y = original.Y + DuplicateOffset,
                Width = original.Width,
                Height = original.Height,
                SymbolType = original.SymbolType
            };

            var control = this.CreateSymbolControl(duplicate);

            WhiteBoardCanvas.Children.Add(control);

            return control;
        }

        private List<Grid> DuplicateSelectedSymbols()
        {
            var duplicates = new List<Grid>();

            foreach (Grid source in _selectedSymbols.ToList())
            {
                Grid? duplicate = DuplicateSymbol(source);

                if (duplicate != null)
                    duplicates.Add(duplicate);
            }

            return duplicates;
        }

        private void SelectDuplicatedElements(List<Grid> shapeDuplicates, List<Grid> textDuplicates, List<Grid> symbolDuplicates)
        {
            this.ClearAllSelections();

            foreach (Grid shape in shapeDuplicates)
            {
                this.AddShapeToSelection(shape);
            }

            foreach (Grid text in textDuplicates)
            {
                this.AddTextToSelection(text);
            }

            foreach (Grid symbol in symbolDuplicates)
            {
                this.AddSymbolToSelection(symbol);
            }

            this._selectedShape = this._selectedShapes.LastOrDefault();
            this._selectedTextElement = this._selectedTextElements.LastOrDefault();
            this._selectedSymbol = this._selectedSymbols.LastOrDefault();
        }

        private ArrowElement? DuplicateArrow(ArrowElement original)
        {
            if (!_duplicateIdMap.TryGetValue(original.SourceId, out Guid newSourceId))
            {
                return null;
            }

            if (!_duplicateIdMap.TryGetValue(original.TargetId, out Guid newTargetId))
            {
                return null;
            }

            return new ArrowElement
            {
                Id = Guid.CreateVersion7(),
                SourceId = newSourceId,
                TargetId = newTargetId
            };
        }

        private List<ArrowElement> DuplicateSelectedArrows()
        {
            var duplicates = new List<ArrowElement>();

            foreach (ArrowElement original in this._arrows.ToList())
            {
                ArrowElement? duplicate = this.DuplicateArrow(original);

                if (duplicate == null)
                {
                    continue;
                }

                this._arrows.Add(duplicate);
                this.DrawArrow(duplicate);

                duplicates.Add(duplicate);
            }

            return duplicates;
        }
    }

    // ============================================================
    // Resize-Richtungen
    // ============================================================

    [Flags]
    public enum ResizeDirection
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