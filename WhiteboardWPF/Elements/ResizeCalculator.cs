namespace WhiteboardWPF.Elements
{
    using System.Windows;

    /// <summary>
    /// Berechnet die neue Geometrie eines Elements während eines Resize-Vorgangs.
    /// </summary>
    public sealed class ResizeCalculator
    {
        public ResizeResult Calculate(
            ResizeDirection direction,
            Point startMousePosition,
            Point currentMousePosition,
            double startX,
            double startY,
            double startWidth,
            double startHeight,
            double minimumWidth,
            double minimumHeight)
        {
            double deltaX = currentMousePosition.X - startMousePosition.X;
            double deltaY = currentMousePosition.Y - startMousePosition.Y;

            double newX = startX;
            double newY = startY;
            double newWidth = startWidth;
            double newHeight = startHeight;

            if (direction.HasFlag(ResizeDirection.Left))
            {
                newWidth = startWidth - deltaX;

                if (newWidth < minimumWidth)
                {
                    newWidth = minimumWidth;
                    newX = startX + (startWidth - minimumWidth);
                }
                else
                {
                    newX = startX + deltaX;
                }
            }

            if (direction.HasFlag(ResizeDirection.Right))
            {
                newWidth = Math.Max(minimumWidth, startWidth + deltaX);
            }

            if (direction.HasFlag(ResizeDirection.Top))
            {
                newHeight = startHeight - deltaY;

                if (newHeight < minimumHeight)
                {
                    newHeight = minimumHeight;
                    newY = startY + (startHeight - minimumHeight);
                }
                else
                {
                    newY = startY + deltaY;
                }
            }

            if (direction.HasFlag(ResizeDirection.Bottom))
            {
                newHeight = Math.Max(minimumHeight, startHeight + deltaY);
            }

            return new ResizeResult(newX, newY, newWidth, newHeight);
        }
    }

    public readonly record struct ResizeResult(double X, double Y, double Width, double Height);
}
