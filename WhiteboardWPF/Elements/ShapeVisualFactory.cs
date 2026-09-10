namespace WhiteboardWPF.Elements
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;

    using WhiteboardWPF.Models;

    /// <summary>
    /// Erzeugt ausschließlich die WPF-Visualisierung eines Shapes.
    /// Auswahl-, Drag-, Resize- und sonstige Interaktionslogik gehört nicht hierher.
    /// </summary>
    public sealed class ShapeVisualFactory
    {
        public FrameworkElement Create(ShapeElement shape, Brush background)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            if (background == null)
            {
                throw new ArgumentNullException(nameof(background));
            }

            switch (shape.ShapeType)
            {
                case ShapeType.Rectangle:
                    return new Border
                    {
                        Background = background,
                        BorderBrush = Brushes.DimGray,
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(0),
                        IsHitTestVisible = true
                    };

                case ShapeType.RoundedRectangle:
                    return new Border
                    {
                        Background = background,
                        BorderBrush = Brushes.DimGray,
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(15),
                        IsHitTestVisible = true
                    };

                case ShapeType.Ellipse:
                    return new Ellipse
                    {
                        Fill = background,
                        Stroke = Brushes.DimGray,
                        StrokeThickness = 2,
                        IsHitTestVisible = true
                    };

                case ShapeType.Diamond:
                    return new Polygon
                    {
                        Fill = background,
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

                case ShapeType.Triangle:
                    return CreateTriangle(background);

                case ShapeType.Hexagon:
                    return CreateHexagon(background);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(shape.ShapeType),
                        shape.ShapeType,
                        "Unbekannter ShapeType.");
            }
        }

        private static FrameworkElement CreateTriangle(Brush background)
        {
            return new Polygon
            {
                Fill = background,
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

        private static FrameworkElement CreateHexagon(Brush background)
        {
            return new Polygon
            {
                Fill = background,
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
    }
}
