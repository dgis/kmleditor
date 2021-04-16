using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KMLEditor
{
    class Utils
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
    }
}
