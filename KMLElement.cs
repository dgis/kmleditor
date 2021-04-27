using System;
using System.ComponentModel;
using System.Drawing;

namespace KMLEditor
{
    public class KMLElement
    {
        public const int selectionBorderMargin = 3;
        public const int selectionInsideMargin = 4;
        public const int minimumSizeWhenResizing = 1;

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
        protected RectangleF rectangle;
        protected RectangleF dragRectangle;
        protected float? downX = null;
        protected float? downY = null;
        protected float? dragDownX = null;
        protected float? dragDownY = null;

        [Description(@"Offset sets the position of the element.")]
        public int OffsetX { get => (int)rectangle.X; set { if (rectangle.X != value) { rectangle.X = value; isDirty = true; } } }
        [Description(@"Offset sets the position of the element.")]
        public int OffsetY { get => (int)rectangle.Y; set { if (rectangle.Y != value) { rectangle.Y = value; isDirty = true; } } }
        public int offsetLineNumber = -1;



        public bool HitTest(Point location)
        {
            if (new RectangleF(OffsetX, OffsetY, rectangle.Width, rectangle.Height).Contains(location)
                || (downX != null && downY != null && new RectangleF((float)downX, (float)downY, rectangle.Width, rectangle.Height).Contains(location)))
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
            Element,
            DownElement
        }

        protected virtual bool AllowPart(SelectionPart part)
        {
            return true;
        }

        public SelectionPart HitTestForSelection(PointF location, float zoom)
        {
            if (GetBoundForPart(SelectionPart.Inside, zoom).Contains(location))
                return SelectionPart.Inside;
            else if (GetBoundForPart(SelectionPart.InsideDown, zoom).Contains(location))
                return SelectionPart.InsideDown;
            else if (GetBoundForPart(SelectionPart.TopLeft, zoom).Contains(location))
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
            if (AllowPart(part))
            {
                float selectionMarginF = Math.Max(1, selectionBorderMargin / zoom);
                float insideSelectionMarginF = Math.Max(1, selectionInsideMargin / zoom);
                switch (part)
                {
                    case SelectionPart.Element:
                        return new RectangleF((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height);
                    case SelectionPart.Inside:
                        return new RectangleF(OffsetX + selectionMarginF + 1, OffsetY + selectionMarginF + 1, 2 * insideSelectionMarginF, 2 * insideSelectionMarginF);
                    case SelectionPart.DownElement:
                        if (downX != null && downY != null)
                            return new RectangleF((float)downX, (float)downY, rectangle.Width, 2 * rectangle.Height);
                        break;
                    case SelectionPart.InsideDown:
                        if (downX != null && downY != null)
                            return new RectangleF((float)(downX) + rectangle.Width - selectionMarginF - 1 - 2 * insideSelectionMarginF, (float)downY + rectangle.Height - selectionMarginF - 1 - 2 * insideSelectionMarginF, 2 * insideSelectionMarginF, 2 * insideSelectionMarginF);
                        break;
                    case SelectionPart.TopLeft:
                        return new RectangleF(OffsetX - selectionMarginF, OffsetY - selectionMarginF, 2 * selectionMarginF, 2 * selectionMarginF);
                    case SelectionPart.Top:
                        return new RectangleF(OffsetX + selectionMarginF, OffsetY - selectionMarginF, rectangle.Width - 2 * selectionMarginF, 2 * selectionMarginF);
                    case SelectionPart.TopRight:
                        return new RectangleF(OffsetX + rectangle.Width - selectionMarginF, OffsetY - selectionMarginF, 2 * selectionMarginF, 2 * selectionMarginF);
                    case SelectionPart.Right:
                        return new RectangleF(OffsetX + rectangle.Width - selectionMarginF, OffsetY + selectionMarginF, 2 * selectionMarginF, rectangle.Height - 2 * selectionMarginF);
                    case SelectionPart.BottomRight:
                        return new RectangleF(OffsetX + rectangle.Width - selectionMarginF, OffsetY + rectangle.Height - selectionMarginF, 2 * selectionMarginF, 2 * selectionMarginF);
                    case SelectionPart.Bottom:
                        return new RectangleF(OffsetX + selectionMarginF, OffsetY + rectangle.Height - selectionMarginF, rectangle.Width - 2 * selectionMarginF, 2 * selectionMarginF);
                    case SelectionPart.BottomLeft:
                        return new RectangleF(OffsetX - selectionMarginF, OffsetY + rectangle.Height - selectionMarginF, 2 * selectionMarginF, 2 * selectionMarginF);
                    case SelectionPart.Left:
                        return new RectangleF(OffsetX - selectionMarginF, OffsetY + selectionMarginF, 2 * selectionMarginF, rectangle.Height - 2 * selectionMarginF);
                }
            }
            return new RectangleF();
        }

        public virtual void Resize(RectangleF before, RectangleF after, bool updateDown)
        {
            rectangle = Utils.Resize(dragRectangle, before, after);

            if (updateDown && this is KMLElementWithOffsetAndSizeAndDown)
            {
                KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)this;
                if (kmlElementWithOffsetAndSizeAndDown.dragDownX != null && kmlElementWithOffsetAndSizeAndDown.dragDownY != null)
                {
                    PointF downResized = Utils.Resize(new PointF((float)kmlElementWithOffsetAndSizeAndDown.dragDownX, (float)kmlElementWithOffsetAndSizeAndDown.dragDownY), before, after);
                    kmlElementWithOffsetAndSizeAndDown.downX = downResized.X;
                    kmlElementWithOffsetAndSizeAndDown.downY = downResized.Y;
                }
            }
        }

        public void BackupBeforeDragging()
        {
            dragRectangle = rectangle;
            dragDownX = downX;
            dragDownY = downY;
        }

        public void ModifyRectangle(Point draggingDelta, SelectionPart draggingPart, bool isCentered, bool keepRatio, bool updateDown)
        {
            float ratio = dragRectangle.Width != 0f ? dragRectangle.Height / dragRectangle.Width : 1f;
            RectangleF newRectangle = new RectangleF();
            switch (draggingPart)
            {
                case SelectionPart.Inside:
                    newRectangle.X = dragRectangle.X + draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y + draggingDelta.Y;
                    newRectangle.Width = dragRectangle.Width;
                    newRectangle.Height = dragRectangle.Height;
                    break;
                case SelectionPart.InsideDown:
                    newRectangle = rectangle;
                    if (downX != null && downY != null)
                    {
                        downX = dragDownX + draggingDelta.X;
                        downY = dragDownY + draggingDelta.Y;
                    }
                    break;
                case SelectionPart.TopLeft:
                    newRectangle.X = dragRectangle.X + draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y + (keepRatio ? ratio * draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width = dragRectangle.Width - (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height - (isCentered ? 2f : 1f) * (keepRatio ? ratio * draggingDelta.X : draggingDelta.Y);
                    break;
                case SelectionPart.Top:
                    newRectangle.X = dragRectangle.X + (keepRatio ? (isCentered ? 1f : 0.5f) * draggingDelta.Y / ratio : 0f);
                    newRectangle.Y = dragRectangle.Y + draggingDelta.Y;
                    newRectangle.Width = dragRectangle.Width - (keepRatio ? (isCentered ? 2f : 1f) * draggingDelta.Y / ratio : 0f);
                    newRectangle.Height = dragRectangle.Height - (isCentered ? 2f : 1f) * draggingDelta.Y;
                    break;
                case SelectionPart.TopRight:
                    newRectangle.X = dragRectangle.X - (isCentered ? 1f : 0f) * draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y + (keepRatio ? -ratio * draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width = dragRectangle.Width + (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height - (isCentered ? 2f : 1f) * (keepRatio ? -ratio * draggingDelta.X : draggingDelta.Y);
                    break;
                case SelectionPart.Right:
                    newRectangle.X = dragRectangle.X - (isCentered ? 1f : 0f) * draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y - (keepRatio ? (isCentered ? 1f : 0.5f) * ratio * draggingDelta.X : 0f);
                    newRectangle.Width = dragRectangle.Width + (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height - (keepRatio ? (isCentered ? 2f : 1f) * -ratio * draggingDelta.X : 0f);
                    break;
                case SelectionPart.BottomRight:
                    newRectangle.X = dragRectangle.X - (isCentered ? 1f : 0f) * draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y - (isCentered ? 1f : 0f) * (keepRatio ? ratio * draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width = dragRectangle.Width + (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height + (isCentered ? 2f : 1f) * (keepRatio ? ratio * draggingDelta.X : draggingDelta.Y);
                    break;
                case SelectionPart.Bottom:
                    newRectangle.X = dragRectangle.X - (keepRatio ? (isCentered ? 1f : 0.5f) * draggingDelta.Y / ratio : 0f);
                    newRectangle.Y = dragRectangle.Y - (isCentered ? 1f : 0f) * draggingDelta.Y;
                    newRectangle.Width = dragRectangle.Width + (keepRatio ? (isCentered ? 2f : 1f) * draggingDelta.Y / ratio : 0f);
                    newRectangle.Height = dragRectangle.Height + (isCentered ? 2f : 1f) * draggingDelta.Y;
                    break;
                case SelectionPart.BottomLeft:
                    newRectangle.X = dragRectangle.X + draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y - (isCentered ? 1f : 0f) * (keepRatio ? -ratio * draggingDelta.X : draggingDelta.Y);
                    newRectangle.Width = dragRectangle.Width - (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height + (isCentered ? 2f : 1f) * (keepRatio ? -ratio * draggingDelta.X : draggingDelta.Y);
                    break;
                case SelectionPart.Left:
                    newRectangle.X = dragRectangle.X + draggingDelta.X;
                    newRectangle.Y = dragRectangle.Y + (keepRatio ? (isCentered ? 1f : 0.5f) * ratio * draggingDelta.X : 0f);
                    newRectangle.Width = dragRectangle.Width - (isCentered ? 2f : 1f) * draggingDelta.X;
                    newRectangle.Height = dragRectangle.Height - (keepRatio ? (isCentered ? 2f : 1f) * ratio * draggingDelta.X : 0f);
                    break;
            }
            if (newRectangle.Width < minimumSizeWhenResizing)
                newRectangle.Width = minimumSizeWhenResizing;
            if (newRectangle.Height < minimumSizeWhenResizing)
                newRectangle.Height = minimumSizeWhenResizing;

            if (this is KMLElementWithOffsetAndSizeAndDown)
            {
                KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)this;
                if (updateDown && kmlElementWithOffsetAndSizeAndDown.downX != null && kmlElementWithOffsetAndSizeAndDown.downY != null)
                {
                    kmlElementWithOffsetAndSizeAndDown.downX += newRectangle.X - rectangle.X;
                    kmlElementWithOffsetAndSizeAndDown.downY += newRectangle.Y - rectangle.Y;
                }
            }
            rectangle = newRectangle;
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
        [Description(@"Size sets the size of the element.")]
        public int SizeWidth { get => (int)rectangle.Width; set { if (rectangle.Width != value) { rectangle.Width = value; isDirty = true; } } }
        [Description(@"Size sets the size of the element.")]
        public int SizeHeight { get => (int)rectangle.Height; set { if (rectangle.Height != value) { rectangle.Height = value; isDirty = true; } } }
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

        public override void Resize(RectangleF before, RectangleF after, bool updateDown)
        {
            rectangle.Location = Utils.Resize(dragRectangle.Location, before, after);
        }
    }

    public class KMLDigit : KMLElementWithOffsetAndSize
    {
        public override string Element { get => "Digit"; }
    }

    public class KMLElementWithOffsetAndSizeAndDown : KMLElementWithOffsetAndSize
    {
        [Description(@"For Annonciator, Down is the position of the annunciator in the bitmap when it is on.
For Button, Down sets the picture of the down button. This is only needed if type 0 is set. Pixels right and pixels down.")]
        public int? DownX { get => (int?)downX; set { if (downX != value) { downX = value; isDirty = true; } } }
        [Description(@"For Annonciator, Down is the position of the annunciator in the bitmap when it is on.
For Button, Down sets the picture of the down button. This is only needed if type 0 is set. Pixels right and pixels down.")]
        public int? DownY { get => (int?)downY; set { if (downY != value) { downY = value; isDirty = true; } } }
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
        [Description(@"Annunciator tells which annunciator you are setting. Valid entries are emulator depending and can vary from 1 through 6, 7, 23, 32 or 60. The annunciator symbol itself depends on the specific LCD of the calculator. Refer to existing KML scripts for getting the symbol of each annunciator.")]
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
        [Description(@"Button tells the number of the button you are creating. This can be any number. Most times the number is chosen representing the row and column number of the button. Any number can be used, but only 256 buttons can be declared. If more buttons are declared, only the first 256 will be used, and the others will be ignored.")]
        public int Number { get => number; set { if (number != value) { number = value; isDirty = true; } } }

        private int type;
        [Description(@"Type tells how the button will behave when it is pressed. Valid entries are 0, 1, 2, 3, 4, or 5.
Type 0 (default) is copying a part of the background bitmap with the dimension Size from the source position Down to the target position Offset.
Type 1 is copying an inner part of the background bitmap with the dimension Size from the source position Offset to the target position Offset one pixel right down. The resulting button gets a new black and white border. This is a default button effect for special designed background bitmaps.
Type 2 does nothing.
Type 3 is drawing a part of the background bitmap with the dimension Size at the target position Offset with inverted colors. This type can be used for a button inside the LCD screen area. The effect on the LCD screen depends on the emulator.
Type 4 is copying a part of the background bitmap with the dimension Size from the source position Offset to the target position Offset showing the background bitmap behind the LCD screen while the key pressed. This type is only useful in Emu48 with disabled option ""Enable Virtual LCD Delay"". On all other emulators this type isn’t working.
Type 5 is drawing a transparent circle inside the rectangle given by Size into the middle of the button area given by target position Offset and the dimension Size.")]
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
