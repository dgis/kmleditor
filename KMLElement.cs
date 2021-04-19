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

        public string Element { get => "[Element]"; }

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
        public Rectangle rectangle;
        public int OffsetX { get => rectangle.X; set { if (rectangle.X != value) { rectangle.X = value; isDirty = true; } } }
        public int OffsetY { get => rectangle.Y; set { if (rectangle.Y != value) { rectangle.Y = value; isDirty = true; } } }
        public int offsetLineNumber = -1;


        public Rectangle dragRectangle;

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
            Inside
        }

        protected virtual bool AllowPart(SelectionPart part)
        {
            return true;
        }

        public SelectionPart HitTestForSelection(Point location)
        {
            if (GetBoundForPart(SelectionPart.Inside).Contains(location))
                return SelectionPart.Inside;
            else if (GetBoundForPart(SelectionPart.Inside).Contains(location))
                return SelectionPart.Inside;
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
                    case SelectionPart.Inside:
                        //return new Rectangle(OffsetX + selectionMargin, OffsetY + selectionMargin, SizeWidth - 2 * selectionMargin, SizeHeight - 2 * selectionMargin);
                        //return new Rectangle(OffsetX + rectangle.Width / 2 - insideSelectionMargin, OffsetY /*+ SizeHeight / 2*/ - insideSelectionMargin, 2 * insideSelectionMargin, 2 * insideSelectionMargin);
                        return new Rectangle(OffsetX + selectionMargin + 1, OffsetY + selectionMargin + 1, 2 * insideSelectionMargin, 2 * insideSelectionMargin);
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
            dragRectangle.X = rectangle.X;
            dragRectangle.Y = rectangle.Y;
            dragRectangle.Width = rectangle.Width;
            dragRectangle.Height = rectangle.Height;
        }

        public void UpdateForDragging(int draggingDeltaX, int draggingDeltaY, SelectionPart draggingPart, bool isShiftPressed, bool updateDown)
        {
            int backupOffsetX = OffsetX;
            int backupOffsetY = OffsetY;

            switch (draggingPart)
            {
                case SelectionPart.Inside:
                    OffsetX = dragRectangle.X + draggingDeltaX;
                    OffsetY = dragRectangle.Y + draggingDeltaY;
                    break;
                case SelectionPart.TopLeft:
                    OffsetX = dragRectangle.X + draggingDeltaX;
                    OffsetY = dragRectangle.Y + draggingDeltaY;
                    if (isShiftPressed)
                    {
                        rectangle.Width = dragRectangle.Width - 2 * draggingDeltaX;
                        rectangle.Height = dragRectangle.Height - 2 * draggingDeltaY;
                    }
                    else
                    {
                        rectangle.Width = dragRectangle.Width - draggingDeltaX;
                        rectangle.Height = dragRectangle.Height - draggingDeltaY;
                    }
                    break;
                case SelectionPart.Top:
                    OffsetY = dragRectangle.Y + draggingDeltaY;
                    if (isShiftPressed)
                        rectangle.Height = dragRectangle.Height - 2 * draggingDeltaY;
                    else
                        rectangle.Height = dragRectangle.Height - draggingDeltaY;
                    break;
                case SelectionPart.TopRight:
                    OffsetY = dragRectangle.Y + draggingDeltaY;
                    if (isShiftPressed)
                    {
                        OffsetX = dragRectangle.X - draggingDeltaX;
                        rectangle.Width = dragRectangle.Width + 2 * draggingDeltaX;
                        rectangle.Height = dragRectangle.Height - 2 * draggingDeltaY;
                    }
                    else
                    {
                        rectangle.Width = dragRectangle.Width + draggingDeltaX;
                        rectangle.Height = dragRectangle.Height - draggingDeltaY;
                    }
                    break;
                case SelectionPart.Right:
                    if (isShiftPressed)
                    {
                        OffsetX = dragRectangle.X - draggingDeltaX;
                        rectangle.Width = dragRectangle.Width + 2 * draggingDeltaX;
                    }
                    else
                        rectangle.Width = dragRectangle.Width + draggingDeltaX;
                    break;
                case SelectionPart.BottomRight:
                    if (isShiftPressed)
                    {
                        OffsetX = dragRectangle.X - draggingDeltaX;
                        OffsetY = dragRectangle.Y - draggingDeltaY;
                        rectangle.Width = dragRectangle.Width + 2 * draggingDeltaX;
                        rectangle.Height = dragRectangle.Height + 2 * draggingDeltaY;
                    }
                    else
                    {
                        rectangle.Width = dragRectangle.Width + draggingDeltaX;
                        rectangle.Height = dragRectangle.Height + draggingDeltaY;
                    }
                    break;
                case SelectionPart.Bottom:
                    if (isShiftPressed)
                    {
                        OffsetY = dragRectangle.Y - draggingDeltaY;
                        rectangle.Height = dragRectangle.Height + 2 * draggingDeltaY;
                    }
                    else
                        rectangle.Height = dragRectangle.Height + draggingDeltaY;
                    break;
                case SelectionPart.BottomLeft:
                    OffsetX = dragRectangle.X + draggingDeltaX;
                    if (isShiftPressed)
                    {
                        OffsetY = dragRectangle.Y - draggingDeltaY;
                        rectangle.Height = dragRectangle.Height + 2 * draggingDeltaY;
                        rectangle.Width = dragRectangle.Width - 2 * draggingDeltaX;
                    }
                    else
                    {
                        rectangle.Width = dragRectangle.Width - draggingDeltaX;
                        rectangle.Height = dragRectangle.Height + draggingDeltaY;
                    }
                    break;
                case SelectionPart.Left:
                    OffsetX = dragRectangle.X + draggingDeltaX;
                    if (isShiftPressed)
                        rectangle.Width = dragRectangle.Width - 2 * draggingDeltaX;
                    else
                        rectangle.Width = dragRectangle.Width - draggingDeltaX;
                    break;
            }
            if (rectangle.Width < minimumSize)
                rectangle.Width = minimumSize;
            if (rectangle.Height < minimumSize)
                rectangle.Height = minimumSize;
            if (updateDown && this is KMLElementWithOffsetAndSizeAndDown)
            {
                KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)this;
                kmlElementWithOffsetAndSizeAndDown.DownX += OffsetX - backupOffsetX;
                kmlElementWithOffsetAndSizeAndDown.DownY += OffsetY - backupOffsetY;
            }
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
        public new string Element { get => "Background"; }
    }

    public class KMLLcd : KMLElementWithOffset
    {
        public KMLLcd() {
            this.rectangle.Width = 50;
            this.rectangle.Height = 50;
        }

        public new string Element { get => "Lcd"; }

        protected override bool AllowPart(SelectionPart part)
        {
            return part == SelectionPart.Inside;
        }
    }

    public class KMLDigit : KMLElementWithOffsetAndSize
    {
        public new string Element { get => "Digit"; }
    }

    public class KMLElementWithOffsetAndSizeAndDown : KMLElementWithOffsetAndSize
    {
        private int? downX = null;
        private int? downY = null;
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
        public new string Element { get => "Annunciator"; }

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
        public new string Element { get => "Button"; }

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
