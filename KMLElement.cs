using System.Drawing;

namespace KMLEditor
{
    public class KMLElement
    {
        public const int selectionMargin = 3;
        public const int insideSelectionMargin = 4;
        public const int minimumSize = 2;

        private static int lastId = -1;

        public int id = ++lastId;

        public virtual string Element { get => "[Element]"; }
        public override string ToString()
        {
            return Element;
        }

        public int elementLineNumber = -1;

        public bool isDirty;
        public KMLFile kmlFile;

        public bool isSelected;

        public virtual KMLElement CopyFrom(KMLElement from)
        {
            id = from.id;
            elementLineNumber = from.elementLineNumber;
            isDirty = from.isDirty;
            kmlFile = from.kmlFile;
            isSelected = from.isSelected;
            return this;
        }
        public virtual KMLElement Clone()
        {
            return new KMLElement().CopyFrom(this);
        }
    }

    public class KMLElementWithOffset : KMLElement
    {
        protected Rectangle rectangle;
        protected Rectangle dragRectangle;
        protected int? downX = null;
        protected int? downY = null;
        protected int? dragDownX = null;
        protected int? dragDownY = null;

        public int OffsetX { get => rectangle.X; set { if (rectangle.X != value) { rectangle.X = value; isDirty = true; } } }
        public int OffsetY { get => rectangle.Y; set { if (rectangle.Y != value) { rectangle.Y = value; isDirty = true; } } }
        public int offsetLineNumber = -1;



        public bool HitTest(Point location)
        {
            if (new Rectangle(OffsetX, OffsetY, rectangle.Width, rectangle.Height).Contains(location))
                return true;
            return false;
        }

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
            Left,
            Inside,
            InsideDown,
            Element
        }

        protected virtual bool AllowPart(SelectionPart part)
        {
            return true;
        }

        public SelectionPart HitTestForSelection(Point location)
        {
            if (GetBoundForPart(SelectionPart.Inside).Contains(location))
                return SelectionPart.Inside;
            else if (GetBoundForPart(SelectionPart.InsideDown).Contains(location))
                return SelectionPart.InsideDown;
            else if (GetBoundForPart(SelectionPart.TopLeft).Contains(location))
                return SelectionPart.TopLeft;
            else if (GetBoundForPart(SelectionPart.Top).Contains(location))
                return SelectionPart.Top;
            else if (GetBoundForPart(SelectionPart.TopRight).Contains(location))
                return SelectionPart.TopRight;
            else if (GetBoundForPart(SelectionPart.Right).Contains(location))
                return SelectionPart.Right;
            else if (GetBoundForPart(SelectionPart.BottomRight).Contains(location))
                return SelectionPart.BottomRight;
            else if (GetBoundForPart(SelectionPart.Bottom).Contains(location))
                return SelectionPart.Bottom;
            else if (GetBoundForPart(SelectionPart.BottomLeft).Contains(location))
                return SelectionPart.BottomLeft;
            else if (GetBoundForPart(SelectionPart.Left).Contains(location))
                return SelectionPart.Left;
            return SelectionPart.None;
        }

        public Rectangle GetBoundForPart(SelectionPart part)
        {
            if (AllowPart(part))
            {
                switch (part)
                {
                    case SelectionPart.Element:
                        return rectangle;
                    case SelectionPart.Inside:
                        return new Rectangle(OffsetX + selectionMargin + 1, OffsetY + selectionMargin + 1, 2 * insideSelectionMargin, 2 * insideSelectionMargin);
                    case SelectionPart.InsideDown:
                        if(downX != null && downY != null)
                            return new Rectangle((int)downX + rectangle.Width - selectionMargin - 1 - 2 * insideSelectionMargin, (int)downY + rectangle.Height - selectionMargin - 1 - 2 * insideSelectionMargin, 2 * insideSelectionMargin, 2 * insideSelectionMargin);
                        break;
                    case SelectionPart.TopLeft:
                        return new Rectangle(OffsetX - selectionMargin, OffsetY - selectionMargin, 2 * selectionMargin, 2 * selectionMargin);
                    case SelectionPart.Top:
                        return new Rectangle(OffsetX + selectionMargin, OffsetY - selectionMargin, rectangle.Width - 2 * selectionMargin, 2 * selectionMargin);
                    case SelectionPart.TopRight:
                        return new Rectangle(OffsetX + rectangle.Width - selectionMargin, OffsetY - selectionMargin, 2 * selectionMargin, 2 * selectionMargin);
                    case SelectionPart.Right:
                        return new Rectangle(OffsetX + rectangle.Width - selectionMargin, OffsetY + selectionMargin, 2 * selectionMargin, rectangle.Height - 2 * selectionMargin);
                    case SelectionPart.BottomRight:
                        return new Rectangle(OffsetX + rectangle.Width - selectionMargin, OffsetY + rectangle.Height - selectionMargin, 2 * selectionMargin, 2 * selectionMargin);
                    case SelectionPart.Bottom:
                        return new Rectangle(OffsetX + selectionMargin, OffsetY + rectangle.Height - selectionMargin, rectangle.Width - 2 * selectionMargin, 2 * selectionMargin);
                    case SelectionPart.BottomLeft:
                        return new Rectangle(OffsetX - selectionMargin, OffsetY + rectangle.Height - selectionMargin, 2 * selectionMargin, 2 * selectionMargin);
                    case SelectionPart.Left:
                        return new Rectangle(OffsetX - selectionMargin, OffsetY + selectionMargin, 2 * selectionMargin, rectangle.Height - 2 * selectionMargin);
                }
            }
            return new Rectangle();
        }

        public void BackupForDragging()
        {
            dragRectangle = rectangle;
            dragDownX = downX;
            dragDownY = downY;
        }

        public void UpdateForDragging(Point draggingDelta, SelectionPart draggingPart, bool isShiftPressed, bool updateDown)
        {
            Rectangle rectangleNew = rectangle;
            switch (draggingPart)
            {
                case SelectionPart.Inside:
                    rectangleNew.X = dragRectangle.X + draggingDelta.X;
                    rectangleNew.Y = dragRectangle.Y + draggingDelta.Y;
                    break;
                case SelectionPart.InsideDown:
                    if(downX != null && downY != null)
                    {
                        downX = dragDownX + draggingDelta.X;
                        downY = dragDownY + draggingDelta.Y;
                    }
                    break;
                case SelectionPart.TopLeft:
                    rectangleNew.X = dragRectangle.X + draggingDelta.X;
                    rectangleNew.Y = dragRectangle.Y + draggingDelta.Y;
                    if (isShiftPressed)
                    {
                        rectangleNew.Width = dragRectangle.Width - 2 * draggingDelta.X;
                        rectangleNew.Height = dragRectangle.Height - 2 * draggingDelta.Y;
                    }
                    else
                    {
                        rectangleNew.Width = dragRectangle.Width - draggingDelta.X;
                        rectangleNew.Height = dragRectangle.Height - draggingDelta.Y;
                    }
                    break;
                case SelectionPart.Top:
                    rectangleNew.Y = dragRectangle.Y + draggingDelta.Y;
                    if (isShiftPressed)
                        rectangleNew.Height = dragRectangle.Height - 2 * draggingDelta.Y;
                    else
                        rectangleNew.Height = dragRectangle.Height - draggingDelta.Y;
                    break;
                case SelectionPart.TopRight:
                    rectangleNew.Y = dragRectangle.Y + draggingDelta.Y;
                    if (isShiftPressed)
                    {
                        rectangleNew.X = dragRectangle.X - draggingDelta.X;
                        rectangleNew.Width = dragRectangle.Width + 2 * draggingDelta.X;
                        rectangleNew.Height = dragRectangle.Height - 2 * draggingDelta.Y;
                    }
                    else
                    {
                        rectangleNew.Width = dragRectangle.Width + draggingDelta.X;
                        rectangleNew.Height = dragRectangle.Height - draggingDelta.Y;
                    }
                    break;
                case SelectionPart.Right:
                    if (isShiftPressed)
                    {
                        rectangleNew.X = dragRectangle.X - draggingDelta.X;
                        rectangleNew.Width = dragRectangle.Width + 2 * draggingDelta.X;
                    }
                    else
                        rectangleNew.Width = dragRectangle.Width + draggingDelta.X;
                    break;
                case SelectionPart.BottomRight:
                    if (isShiftPressed)
                    {
                        rectangleNew.X = dragRectangle.X - draggingDelta.X;
                        rectangleNew.Y = dragRectangle.Y - draggingDelta.Y;
                        rectangleNew.Width = dragRectangle.Width + 2 * draggingDelta.X;
                        rectangleNew.Height = dragRectangle.Height + 2 * draggingDelta.Y;
                    }
                    else
                    {
                        rectangleNew.Width = dragRectangle.Width + draggingDelta.X;
                        rectangleNew.Height = dragRectangle.Height + draggingDelta.Y;
                    }
                    break;
                case SelectionPart.Bottom:
                    if (isShiftPressed)
                    {
                        rectangleNew.Y = dragRectangle.Y - draggingDelta.Y;
                        rectangleNew.Height = dragRectangle.Height + 2 * draggingDelta.Y;
                    }
                    else
                        rectangleNew.Height = dragRectangle.Height + draggingDelta.Y;
                    break;
                case SelectionPart.BottomLeft:
                    rectangleNew.X = dragRectangle.X + draggingDelta.X;
                    if (isShiftPressed)
                    {
                        rectangleNew.Y = dragRectangle.Y - draggingDelta.Y;
                        rectangleNew.Height = dragRectangle.Height + 2 * draggingDelta.Y;
                        rectangleNew.Width = dragRectangle.Width - 2 * draggingDelta.X;
                    }
                    else
                    {
                        rectangleNew.Width = dragRectangle.Width - draggingDelta.X;
                        rectangleNew.Height = dragRectangle.Height + draggingDelta.Y;
                    }
                    break;
                case SelectionPart.Left:
                    rectangleNew.X = dragRectangle.X + draggingDelta.X;
                    if (isShiftPressed)
                        rectangleNew.Width = dragRectangle.Width - 2 * draggingDelta.X;
                    else
                        rectangleNew.Width = dragRectangle.Width - draggingDelta.X;
                    break;
            }
            if (rectangleNew.Width < minimumSize)
                rectangleNew.Width = minimumSize;
            if (rectangleNew.Height < minimumSize)
                rectangleNew.Height = minimumSize;

            if (this is KMLElementWithOffsetAndSizeAndDown)
            {
                KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)this;
                if (updateDown)
                {
                    kmlElementWithOffsetAndSizeAndDown.DownX += rectangleNew.X - rectangle.X;
                    kmlElementWithOffsetAndSizeAndDown.DownY += rectangleNew.Y - rectangle.Y;
                }
            }
            rectangle = rectangleNew;
        }

        public override KMLElement CopyFrom(KMLElement from)
        {
            base.CopyFrom(from);
            KMLElementWithOffset fromWithOffset = from as KMLElementWithOffset;
            if(fromWithOffset != null)
            {
                rectangle = fromWithOffset.rectangle;
                offsetLineNumber = fromWithOffset.offsetLineNumber;
                dragRectangle = fromWithOffset.dragRectangle;
            }
            return this;
        }
        public override KMLElement Clone()
        {
            return new KMLElementWithOffset().CopyFrom(this);
        }
    }

    public class KMLElementWithOffsetAndSize : KMLElementWithOffset
    {
        public int SizeWidth { get => rectangle.Width; set { if (rectangle.Width != value) { rectangle.Width = value; isDirty = true; } } }
        public int SizeHeight { get => rectangle.Height; set { if (rectangle.Height != value) { rectangle.Height = value; isDirty = true; } } }
        public int sizeLineNumber = -1;

        public override KMLElement CopyFrom(KMLElement from)
        {
            base.CopyFrom(from);
            KMLElementWithOffsetAndSize fromWithOffsetAndSize = from as KMLElementWithOffsetAndSize;
            if (fromWithOffsetAndSize != null)
            {
                sizeLineNumber = fromWithOffsetAndSize.sizeLineNumber;
            }
            return this;
        }
        public override KMLElement Clone()
        {
            return new KMLElementWithOffsetAndSize().CopyFrom(this);
        }
    }

    public class KMLBackground : KMLElementWithOffsetAndSize
    {
        public override string Element { get => "Background"; }
    }

    public class KMLLcd : KMLElementWithOffset
    {
        public KMLLcd() {
            this.rectangle.Width = 50;
            this.rectangle.Height = 50;
        }

        public override string Element { get => "Lcd"; }

        protected override bool AllowPart(SelectionPart part)
        {
            return part == SelectionPart.Inside || part == SelectionPart.Element;
        }
    }

    public class KMLDigit : KMLElementWithOffsetAndSize
    {
        public override string Element { get => "Digit"; }
    }

    public class KMLElementWithOffsetAndSizeAndDown : KMLElementWithOffsetAndSize
    {
        public int? DownX { get => downX; set { if (downX != value) { downX = value; isDirty = true; } } }
        public int? DownY { get => downY; set { if (downY != value) { downY = value; isDirty = true; } } }
        public int downLineNumber = -1;

        public override KMLElement CopyFrom(KMLElement from)
        {
            base.CopyFrom(from);
            KMLElementWithOffsetAndSizeAndDown fromWithOffsetAndSizeAndDown = from as KMLElementWithOffsetAndSizeAndDown;
            if (fromWithOffsetAndSizeAndDown != null)
            {
                downX = fromWithOffsetAndSizeAndDown.downX;
                downY = fromWithOffsetAndSizeAndDown.downY;
                downLineNumber = fromWithOffsetAndSizeAndDown.downLineNumber;
            }
            return this;
        }
        public override KMLElement Clone()
        {
            return new KMLElementWithOffsetAndSizeAndDown().CopyFrom(this);
        }
    }

    public class KMLAnnunciator : KMLElementWithOffsetAndSizeAndDown
    {
        public override string Element { get => "Annunciator"; }
        public override string ToString()
        {
            return base.ToString() + " " + Number;
        }


        private int number;
        public int Number { get => number; set { if (number != value) { number = value; isDirty = true; } } }

        public override KMLElement CopyFrom(KMLElement from)
        {
            base.CopyFrom(from);
            KMLAnnunciator fromAnnunciator = from as KMLAnnunciator;
            if (fromAnnunciator != null)
            {
                number = fromAnnunciator.number;
            }
            return this;
        }
        public override KMLElement Clone()
        {
            return new KMLAnnunciator().CopyFrom(this);
        }
    }

    public class KMLButton : KMLElementWithOffsetAndSizeAndDown
    {
        public override string Element { get => "Button"; }
        public override string ToString()
        {
            return base.ToString() + " " + Number;
        }

        private int number;
        public int Number { get => number; set { if (number != value) { number = value; isDirty = true; } } }

        private int type;
        public int Type { get => type; set { if (type != value) { type = value; isDirty = true; } } }
        public int typeLineNumber = -1;

        public override KMLElement CopyFrom(KMLElement from)
        {
            base.CopyFrom(from);
            KMLButton fromButton = from as KMLButton;
            if (fromButton != null)
            {
                number = fromButton.number;
                type = fromButton.type;
                typeLineNumber = fromButton.typeLineNumber;
            }
            return this;
        }
        public override KMLElement Clone()
        {
            return new KMLButton().CopyFrom(this);
        }
    }
}
