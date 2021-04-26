using System;
using System.Drawing;

namespace KMLEditor
{
    class MultipleSelection
    {
        private const int selectionBorderMargin = 3;
        private const int selectionBorderSize = 5;

        public RectangleF rectangle;
        public RectangleF dragRectangle;
        public enum SelectionPart
        {
            None,
            TopLeft,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft,
            Left
        }

        public SelectionPart HitTestForSelection(PointF location, float zoom)
        {
            if (GetBoundForPart(SelectionPart.TopLeft, zoom).Contains(location))
                return SelectionPart.TopLeft;
            else if (GetBoundForPart(SelectionPart.Top, zoom).Contains(location))
                return SelectionPart.Top;
            else if (GetBoundForPart(SelectionPart.TopRight, zoom).Contains(location))
                return SelectionPart.TopRight;
            else if (GetBoundForPart(SelectionPart.Right, zoom).Contains(location))
                return SelectionPart.Right;
            else if (GetBoundForPart(SelectionPart.BottomRight, zoom).Contains(location))
                return SelectionPart.BottomRight;
            else if (GetBoundForPart(SelectionPart.Bottom, zoom).Contains(location))
                return SelectionPart.Bottom;
            else if (GetBoundForPart(SelectionPart.BottomLeft, zoom).Contains(location))
                return SelectionPart.BottomLeft;
            else if (GetBoundForPart(SelectionPart.Left, zoom).Contains(location))
                return SelectionPart.Left;
            return SelectionPart.None;
        }

        public RectangleF GetBoundForPart(SelectionPart part, float zoom)
        {
            float selectionMarginF = Math.Max(1, selectionBorderMargin / zoom);
            float selectionBorderSizeF = Math.Max(1, selectionBorderSize / zoom);
            switch (part)
            {
                case SelectionPart.TopLeft:
                    return new RectangleF(rectangle.X - selectionMarginF - selectionBorderSizeF, rectangle.Y - selectionMarginF - selectionBorderSizeF, selectionBorderSizeF, selectionBorderSizeF);
                case SelectionPart.Top:
                    return new RectangleF(rectangle.X + (rectangle.Width - selectionBorderSizeF) / 2f, rectangle.Y - selectionMarginF - selectionBorderSizeF, selectionBorderSizeF, selectionBorderSizeF);
                case SelectionPart.TopRight:
                    return new RectangleF(rectangle.X + rectangle.Width + selectionMarginF, rectangle.Y - selectionMarginF - selectionBorderSizeF, selectionBorderSizeF, selectionBorderSizeF);
                case SelectionPart.Right:
                    return new RectangleF(rectangle.X + rectangle.Width + selectionMarginF, rectangle.Y + (rectangle.Height - selectionBorderSizeF) / 2f, selectionBorderSizeF, selectionBorderSizeF);
                case SelectionPart.BottomRight:
                    return new RectangleF(rectangle.X + rectangle.Width + selectionMarginF, rectangle.Y + rectangle.Height + selectionMarginF, selectionBorderSizeF, selectionBorderSizeF);
                case SelectionPart.Bottom:
                    return new RectangleF(rectangle.X + (rectangle.Width - selectionBorderSizeF) / 2f, rectangle.Y + rectangle.Height + selectionMarginF, selectionBorderSizeF, selectionBorderSizeF);
                case SelectionPart.BottomLeft:
                    return new RectangleF(rectangle.X - selectionMarginF - selectionBorderSizeF, rectangle.Y + rectangle.Height + selectionMarginF, selectionBorderSizeF, selectionBorderSizeF);
                case SelectionPart.Left:
                    return new RectangleF(rectangle.X - selectionMarginF - selectionBorderSizeF, rectangle.Y + (rectangle.Height - selectionBorderSizeF) / 2f, selectionBorderSizeF, selectionBorderSizeF);
            }
            return new RectangleF();
        }
        public void BackupBeforeDragging()
        {
            dragRectangle = rectangle;
        }

        public RectangleF ModifyRectangle(PointF draggingDelta, SelectionPart draggingMultipleSelectionPart, bool isCentered, bool keepRatio)
        {
            RectangleF newRectangle = dragRectangle;
            switch (draggingMultipleSelectionPart)
            {
                case MultipleSelection.SelectionPart.TopLeft:
                    newRectangle.X += draggingDelta.X;
                    newRectangle.Y += keepRatio ? draggingDelta.X : draggingDelta.Y;
                    newRectangle.Width -= (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height -= (isCentered ? 2f : 1f) * (keepRatio ? draggingDelta.X : draggingDelta.Y);
                    break;
                case MultipleSelection.SelectionPart.Top:
                    if (keepRatio)
                    {
                        newRectangle.X += 0.5f * draggingDelta.Y;
                        newRectangle.Width -= (isCentered ? 2f : 1f) * draggingDelta.Y;
                    }
                    newRectangle.Y += draggingDelta.Y;
                    newRectangle.Height -= (isCentered ? 2f : 1f) * draggingDelta.Y;
                    break;
                case MultipleSelection.SelectionPart.TopRight:
                    if(isCentered)
                        newRectangle.X -= draggingDelta.X;
                    newRectangle.Y += keepRatio ? -draggingDelta.X : draggingDelta.Y;
                    newRectangle.Width += (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height -= (isCentered ? 2f : 1f) * (keepRatio ? -draggingDelta.X : draggingDelta.Y);
                    break;
                case MultipleSelection.SelectionPart.Right:
                    if (keepRatio)
                    {
                        newRectangle.Y -= (isCentered ? 1f : 0.5f) * draggingDelta.X;
                        newRectangle.Height -= (isCentered ? 2f : 1f) * -draggingDelta.X;
                    }
                    if(isCentered)
                        newRectangle.X -= draggingDelta.X;
                    newRectangle.Width += (isCentered ? 2f : 1f) * draggingDelta.X;
                    break;
                case MultipleSelection.SelectionPart.BottomRight:
                    if (isCentered)
                    {
                        newRectangle.X -= draggingDelta.X;
                        newRectangle.Y -= keepRatio ? draggingDelta.X : draggingDelta.Y;
                    }
                    newRectangle.Width += (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height += (isCentered ? 2f : 1f) * (keepRatio ? draggingDelta.X : draggingDelta.Y);
                    break;
                case MultipleSelection.SelectionPart.Bottom:
                    if (keepRatio)
                    {
                        newRectangle.X -= draggingDelta.Y * (isCentered ? 1f : 0.5f);
                        newRectangle.Width += (isCentered ? 2f : 1f) * draggingDelta.Y;
                    }
                    if (isCentered)
                        newRectangle.Y -= draggingDelta.Y;
                    newRectangle.Height += (isCentered ? 2f : 1f) * draggingDelta.Y;
                    break;
                case MultipleSelection.SelectionPart.BottomLeft:
                    newRectangle.X += draggingDelta.X;
                    if (isCentered)
                        newRectangle.Y -= (keepRatio ? -draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width -= (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height += (isCentered ? 2f : 1f) * (keepRatio ? -draggingDelta.X : draggingDelta.Y);
                    break;
                case MultipleSelection.SelectionPart.Left:
                    if (keepRatio)
                    {
                        newRectangle.Y += (isCentered ? 1f : 0.5f) * draggingDelta.X;
                        newRectangle.Height -= (isCentered ? 2f : 1f) * draggingDelta.X;
                    }
                    newRectangle.X += draggingDelta.X;
                    newRectangle.Width -= (isCentered ? 2f : 1f) * draggingDelta.X;
                    break;
            }
            return newRectangle;
        }
    }
}
