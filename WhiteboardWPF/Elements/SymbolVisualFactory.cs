namespace WhiteboardWPF.Elements
{
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Erzeugt die DrawingImage-Visualisierung für Symbole.
    /// Die Symboltypen entsprechen den in der bestehenden ElementLibrary
    /// registrierten Symbolen.
    /// </summary>
    public sealed class SymbolVisualFactory
    {
        public DrawingImage Create(string symbolType)
        {
            return symbolType switch
            {
                "Info" => CreateInfoSymbol(),
                "Warning" => CreateWarningSymbol(),
                "Question" => CreateQuestionSymbol(),
                "Equals" => CreateEqualsSymbol(),
                "NotEquals" => CreateNotEqualsSymbol(),

                _ => CreateInfoSymbol()
            };
        }

        private DrawingImage CreateInfoSymbol()
        {
            var drawingGroup = new DrawingGroup();

            var circleGeometry =
                new EllipseGeometry(new Point(40, 40), 36, 36);

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.DodgerBlue, new Pen(Brushes.DodgerBlue, 1), circleGeometry));

            var formattedText = new FormattedText(
                "i",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    FontWeights.Bold,
                    FontStretches.Normal),
                42,
                Brushes.White,
                1.0);

            Geometry textGeometry = formattedText.BuildGeometry(new Point(35, 7));

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.White, null, textGeometry));

            return new DrawingImage(drawingGroup);
        }

        private DrawingImage CreateWarningSymbol()
        {
            var drawingGroup = new DrawingGroup();

            var geometry = Geometry.Parse("M 40,5 L 75,70 L 5,70 Z");

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Gold, new Pen(Brushes.Black, 2), geometry));

            var formattedText = new FormattedText(
                "!",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    FontWeights.Bold,
                    FontStretches.Normal),
                42,
                Brushes.Black,
                1.0);

            Geometry textGeometry = formattedText.BuildGeometry(new Point(35, 18));

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Black, null, textGeometry));

            return new DrawingImage(drawingGroup);
        }

        private DrawingImage CreateQuestionSymbol()
        {
            var drawingGroup = new DrawingGroup();

            var circleGeometry = new EllipseGeometry(new Point(40, 40), 36, 36);

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.DodgerBlue, new Pen(Brushes.DodgerBlue, 1), circleGeometry));

            var formattedText = new FormattedText(
                "?",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    FontWeights.Bold,
                    FontStretches.Normal),
                42,
                Brushes.White,
                1.0);

            Geometry textGeometry = formattedText.BuildGeometry(new Point(28, 7));

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.White, null, textGeometry));

            return new DrawingImage(drawingGroup);
        }

        private DrawingImage CreateEqualsSymbol()
        {
            var drawingGroup = new DrawingGroup();

            var circleGeometry = new EllipseGeometry(new Point(40, 40), 36, 36);

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.DodgerBlue, new Pen(Brushes.DodgerBlue, 1), circleGeometry));

            var formattedText = new FormattedText(
                "=",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    FontWeights.Bold,
                    FontStretches.Normal),
                42,
                Brushes.White,
                1.0);

            Geometry textGeometry = formattedText.BuildGeometry(new Point(25, 7));

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.White, null, textGeometry));

            return new DrawingImage(drawingGroup);
        }

        private DrawingImage CreateNotEqualsSymbol()
        {
            var drawingGroup = new DrawingGroup();

            var circleGeometry = new EllipseGeometry(new Point(40, 40), 36, 36);

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.DodgerBlue, new Pen(Brushes.DodgerBlue, 1), circleGeometry));

            var formattedText = new FormattedText(
                "≠",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    FontWeights.Bold,
                    FontStretches.Normal),
                42,
                Brushes.White,
                1.0);

            Geometry textGeometry = formattedText.BuildGeometry(new Point(25, 7));

            drawingGroup.Children.Add(new GeometryDrawing(Brushes.White, null, textGeometry));

            return new DrawingImage(drawingGroup);
        }
    }
}
