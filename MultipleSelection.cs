//    KMLEditor - A small KML files editor to edit the skins for the emulators like Emu48.
//    Copyright (C) 2021 Regis COSNIER
//
//    This program is free software; you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation; either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program; if not, write to the Free Software
//    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Drawing;

namespace KMLEditor
{
    class MultipleSelection
    {
        private const int selectionBorderMargin = 3;
        private const int selectionBorderSize = 6;

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
            float ratio = dragRectangle.Width != 0f ? dragRectangle.Height / dragRectangle.Width : 1f;
            RectangleF newRectangle = new RectangleF();
            switch (draggingMultipleSelectionPart)
            {
                case MultipleSelection.SelectionPart.TopLeft:
                    newRectangle.X = dragRectangle.X + draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y + (keepRatio ? ratio * draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width = dragRectangle.Width - (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height - (isCentered ? 2f : 1f) * (keepRatio ? ratio * draggingDelta.X : draggingDelta.Y);
                    break;
                case MultipleSelection.SelectionPart.Top:
                    newRectangle.X = dragRectangle.X + (keepRatio ? (isCentered ? 1f : 0.5f) * draggingDelta.Y / ratio : 0f);
                    newRectangle.Y = dragRectangle.Y + draggingDelta.Y;
                    newRectangle.Width = dragRectangle.Width - (keepRatio ? (isCentered ? 2f : 1f) * draggingDelta.Y / ratio : 0f);
                    newRectangle.Height = dragRectangle.Height - (isCentered ? 2f : 1f) * draggingDelta.Y;
                    break;
                case MultipleSelection.SelectionPart.TopRight:
                    newRectangle.X = dragRectangle.X - (isCentered ? 1f : 0f) * draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y + (keepRatio ? -ratio * draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width = dragRectangle.Width + (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height - (isCentered ? 2f : 1f) * (keepRatio ? -ratio * draggingDelta.X : draggingDelta.Y);
                    break;
                case MultipleSelection.SelectionPart.Right:
                    newRectangle.X = dragRectangle.X - (isCentered ? 1f : 0f) * draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y - (keepRatio ? (isCentered ? 1f : 0.5f) * ratio * draggingDelta.X : 0f);
                    newRectangle.Width = dragRectangle.Width + (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height - (keepRatio ? (isCentered ? 2f : 1f) * -ratio * draggingDelta.X : 0f);
                    break;
                case MultipleSelection.SelectionPart.BottomRight:
                    newRectangle.X = dragRectangle.X - (isCentered ? 1f : 0f) * draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y - (isCentered ? 1f : 0f) * (keepRatio ? ratio * draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width = dragRectangle.Width + (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height + (isCentered ? 2f : 1f) * (keepRatio ? ratio * draggingDelta.X : draggingDelta.Y);
                    break;
                case MultipleSelection.SelectionPart.Bottom:
                    newRectangle.X = dragRectangle.X - (keepRatio ? (isCentered ? 1f : 0.5f) * draggingDelta.Y / ratio : 0f);
                    newRectangle.Y = dragRectangle.Y - (isCentered ? 1f : 0f) * draggingDelta.Y;
                    newRectangle.Width = dragRectangle.Width + (keepRatio ? (isCentered ? 2f : 1f) * draggingDelta.Y / ratio : 0f);
                    newRectangle.Height = dragRectangle.Height + (isCentered ? 2f : 1f) * draggingDelta.Y;
                    break;
                case MultipleSelection.SelectionPart.BottomLeft:
                    newRectangle.X = dragRectangle.X + draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y - (isCentered ? 1f : 0f) * (keepRatio ? -ratio * draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width = dragRectangle.Width - (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height + (isCentered ? 2f : 1f) * (keepRatio ? -ratio * draggingDelta.X : draggingDelta.Y);
                    break;
                case MultipleSelection.SelectionPart.Left:
                    newRectangle.X = dragRectangle.X + draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y + (keepRatio ? (isCentered ? 1f : 0.5f) * ratio * draggingDelta.X : 0f);
                    newRectangle.Width = dragRectangle.Width - (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height - (keepRatio ? (isCentered ? 2f : 1f) * ratio * draggingDelta.X : 0f);
                    break;
            }
            return newRectangle;
        }
    }
}
