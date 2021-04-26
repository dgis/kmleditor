using System;
using System.Drawing;
using System.Windows.Forms;

namespace KMLEditor
{
    static class Utils
    {
        public static int ParseInteger(string number, int defaultValue) {
            int result = defaultValue;
            int.TryParse(number, out result);
            return result;
        }

        public static int GetNumberOfVisibleLines(TextBox textBox)
        {
            try
            {
                int topIndex = textBox.GetCharIndexFromPosition(new Point(5, 5));
                int bottomIndex = textBox.GetCharIndexFromPosition(new Point(5, textBox.Height - textBox.PreferredHeight - 5));
                int topLine = textBox.GetLineFromCharIndex(topIndex);
                int bottomLine = textBox.GetLineFromCharIndex(bottomIndex);
                return bottomLine - topLine + 1;
            }
            catch (Exception)
            {
            }
            return 0;
        }

        public static void DrawRectangle(this Graphics graphics, Pen pen, RectangleF rectangle)
        {
            graphics.DrawRectangle(pen, rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        public static PointF Resize(PointF p, RectangleF ra, RectangleF rb)
        {
            if (ra.Width == 0f || ra.Height == 0f)
                return p;
            // ratio = (p.X - ra.X1) / (ra.X2 - ra.X1) = (p.x' - rb.X1) / (rb.X2 - rb.X1) = (p.X - ra.X1) / ra.Width = (p.X' - rb.X1) / rb.Width
            // p.X' = rb.Width * (p.X - ra.X1) / ra.Width + rb.X1
            return new PointF(rb.Width * (p.X - ra.X) / ra.Width + rb.X, rb.Height * (p.Y - ra.Y) / ra.Height + rb.Y);
        }

        public static RectangleF Resize(RectangleF r, RectangleF ra, RectangleF rb)
        {
            if (ra.Width == 0f || ra.Height == 0f)
                return r;
            // ratio = (r.X - ra.X1) / (ra.X2 - ra.X1) = (r.x' - rb.X1) / (rb.X2 - rb.X1) = (r.X - ra.X1) / ra.Width = (r.X' - rb.X1) / rb.Width
            // r.X' = rb.Width * (r.X - ra.X1) / ra.Width + rb.X1
            // r.W = r.X2' - r.X1' = rb.Width * (r.X2 - ra.X1) / ra.Width + rb.X1 - (rb.Width * (r.X1 - ra.X1) / ra.Width + rb.X1)
            // r.W = rb.Width * (r.X2 - r.X1) / ra.Width
            float rW = rb.Width / ra.Width;
            float rH = rb.Height / ra.Height;
            return new RectangleF(rW * (r.X - ra.X) + rb.X, rH * (r.Y - ra.Y) + rb.Y, rW * r.Width, rH * r.Height);
        }
    }
}
