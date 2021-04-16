using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KMLEditor
{
    public partial class KMLEditorForm : Form
    {
        private KMLFileManager kmlFileManager = new KMLFileManager();
        private float zoom = 1.0f;
        private Bitmap bitmap;
        class UIElementForKMLFile {
            public TabPage tabPage;
            public TextBox textBox;
        };
        Dictionary<KMLFile, UIElementForKMLFile> uiElementPerKMLFile = new Dictionary<KMLFile, UIElementForKMLFile>();
        List<KMLElementWithOffset> selectedElements = new List<KMLElementWithOffset>();
        List<KMLElementWithOffset> selectedElementsBeforeRectangleSelection = new List<KMLElementWithOffset>();

        KMLElementWithOffset draggingElement = null;
        KMLElementWithOffset.SelectionPart draggingPart = KMLElementWithOffset.SelectionPart.None;
        Point draggingStartLocation;

        private static SolidBrush foreground = new SolidBrush(Color.DarkGoldenrod);
        private static Pen foregroundPen = new Pen(foreground);
        private static Pen foregroundButtonDownPen = new Pen(foreground);
        private static SolidBrush foregroundSelected = new SolidBrush(Color.Red);
        private static SolidBrush foregroundButtonSelected = new SolidBrush(Color.Pink);
        private static SolidBrush selectionColor = new SolidBrush(Color.Gray);
        private static Pen foregroundSelectedPen = new Pen(foregroundSelected);
        private static Pen foregroundButtonSelectedPen = new Pen(foregroundButtonSelected);
        private static Pen foregroundButtonDownSelectedPen = new Pen(foregroundSelected);
        private static Pen selectionPen = new Pen(selectionColor);
        private Point selectionRectangleStartLocation = new Point();
        private Rectangle selectionRectangleScaled = new Rectangle();
        private bool showSelectionRectangle = false;
        private bool leftMouseButtonDownPulse = false;

        public KMLEditorForm()
        {
            InitializeComponent();

            scrollingControlContainer.VirtualSize = new Size(0, 0);
            scrollingControlContainer.ScrolledControl.MouseWheel += ScrolledControl_MouseWheel;
            scrollingControlContainer.ContentMouseDown += ScrolledControl_MouseDown;
            scrollingControlContainer.ContentMouseMove += ScrolledControl_MouseMove;
            scrollingControlContainer.ContentMouseUp += ScrolledControl_MouseUp;
            scrollingControlContainer.ScrolledControl.Paint += ScrolledControl_Paint;

            selectionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            foregroundButtonDownPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            foregroundButtonDownSelectedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        }

        private void KMLEditorForm_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && kmlFileManager.AddKMLFile(args[1]))
                InitializeForDisplay();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Cleanup();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "kml files (*.kml)|*.kml|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK && kmlFileManager.AddKMLFile(openFileDialog.FileName))
                InitializeForDisplay();
        }

        private void UpdateSourceCodeTabPageTitle(KMLFile kmlFile)
        {
            UIElementForKMLFile uiElement = uiElementPerKMLFile[kmlFile];
            if(uiElement != null)
            {
                TabPage tabPage = uiElement.tabPage;
                tabPage.Text = kmlFile.GetFilenameOnly() + (kmlFile.isDirty ? "*" : "");
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var item in uiElementPerKMLFile)
            {
                KMLFile kmlFile = item.Key;
                if (kmlFile.isDirty)
                {
                    if (kmlFile.WriteFile(null))
                        UpdateSourceCodeTabPageTitle(kmlFile);
                    else
                        MessageBox.Show("Cannot save this file!");
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "kml files (*.kml)|*.kml|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                KMLFile kmlFileRoot = kmlFileManager.GetRootFile();
                if (kmlFileRoot != null)
                {
                    if(kmlFileRoot.WriteFile(saveFileDialog.FileName))
                    {
                        UpdateSourceCodeTabPageTitle(kmlFileRoot);
                        MessageBox.Show("Only the main KML file has been saved!");
                    }
                    else
                        MessageBox.Show("Cannot save this file!");
                }
            }
        }

        private void toolStripButtonUpdateKMLSource_Click(object sender, EventArgs e)
        {
            kmlFileManager.UpdateKMLSource();
            foreach (var item in uiElementPerKMLFile)
            {
                KMLFile kmlFile = item.Key;
                UpdateSourceCodeTabPageTitle(kmlFile);

                TextBox textBox = item.Value.textBox;
                int selectionStart = textBox.SelectionStart;
                int selectionLength = textBox.SelectionLength;
                textBox.Text = kmlFile.GetSourceCode();
                textBox.SelectionStart = textBox.GetFirstCharIndexFromLine(textBox.GetLineFromCharIndex(selectionStart) + Utils.GetNumberOfVisibleLines(textBox) / 2);
                textBox.ScrollToCaret();
                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = selectionLength;
                textBox.ScrollToCaret();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ScrolledControl_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            Point virtualPoint = scrollingControlContainer.VirtualPoint;
            e.Graphics.TranslateTransform(-(float)virtualPoint.X, -(float)virtualPoint.Y);
            e.Graphics.ScaleTransform(zoom, zoom);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (bitmap != null)
                graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            KMLElementWithOffset lastSelectedElement = GetLastSelectedElement();
            foreach (var element in kmlFileManager.GetElementsWithOffsetAndSize())
            {
                Pen pen = element.isSelected? foregroundSelectedPen : foregroundPen;
                if (element == lastSelectedElement)
                {
                    if(element is KMLLcd)
                    {
                        graphics.DrawLine(foregroundButtonSelectedPen, element.rectangle.Left, element.rectangle.Top, element.rectangle.Right, element.rectangle.Top);
                        graphics.DrawLine(foregroundButtonSelectedPen, element.rectangle.Left, element.rectangle.Top, element.rectangle.Left, element.rectangle.Bottom);
                    }
                    else
                    {
                        graphics.DrawRectangle(foregroundButtonSelectedPen, element.rectangle);
                        graphics.DrawRectangle(foregroundButtonSelectedPen, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.TopLeft));
                        graphics.DrawRectangle(foregroundButtonSelectedPen, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.TopRight));
                        graphics.DrawRectangle(foregroundButtonSelectedPen, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.BottomRight));
                        graphics.DrawRectangle(foregroundButtonSelectedPen, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.BottomLeft));
                    }
                    graphics.DrawRectangle(foregroundButtonSelectedPen, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.Inside));
                }
                else if (element is KMLLcd)
                {
                    graphics.DrawLine(pen, element.rectangle.Left, element.rectangle.Top, element.rectangle.Right, element.rectangle.Top);
                    graphics.DrawLine(pen, element.rectangle.Left, element.rectangle.Top, element.rectangle.Left, element.rectangle.Bottom);
                }
                else
                    graphics.DrawRectangle(pen, element.rectangle);

                if (element is KMLElementWithOffsetAndSizeAndDown)
                {
                    KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)element;
                    if (kmlElementWithOffsetAndSizeAndDown.DownX != null && kmlElementWithOffsetAndSizeAndDown.DownY != null)
                        graphics.DrawRectangle(element.isSelected ? foregroundButtonDownSelectedPen : foregroundButtonDownPen,
                            new Rectangle((int)kmlElementWithOffsetAndSizeAndDown.DownX, (int)kmlElementWithOffsetAndSizeAndDown.DownY, kmlElementWithOffsetAndSizeAndDown.SizeWidth, kmlElementWithOffsetAndSizeAndDown.SizeHeight));
                }
            }

            if(showSelectionRectangle)
                graphics.DrawRectangle(selectionPen, selectionRectangleScaled);
        }

        private void ScrolledControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.Delta > 0)
                {
                    zoom *= 1.25f;
                    if (zoom > 4f)
                        zoom = 4f;
                }
                else
                {
                    zoom /= 1.25f;
                    if (zoom < 0.125f)
                        zoom = 0.125f;
                }

                Point virtualCenter = this.scrollingControlContainer.VirtualPoint;
                virtualCenter.Offset(this.scrollingControlContainer.ScrolledControl.Width / 2, this.scrollingControlContainer.ScrolledControl.Height / 2);
                Size oldSize = this.scrollingControlContainer.VirtualSize;
                scrollingControlContainer.VirtualSize = new Size((int)(bitmap.Width * zoom), (int)(bitmap.Height * zoom));

                Size newSize = this.scrollingControlContainer.VirtualSize;
                Point newVirtualCenter = new Point();
                newVirtualCenter.X = (int)((float)newSize.Width / (float)oldSize.Width * (float)virtualCenter.X);
                newVirtualCenter.Y = (int)((float)newSize.Height / (float)oldSize.Height * (float)virtualCenter.Y);
                this.scrollingControlContainer.ScrollTo(newVirtualCenter, true);
                InvalidateDisplay();
            }
        }

        private bool ScrolledControl_MouseDown(object sender, MouseEventArgs e)
        {
            //Trace.WriteLine(string.Format("ScrolledControl_MouseDown"));

            if (e.Button != MouseButtons.Left)
                return false;

            Point location = new Point((int)((e.Location.X + scrollingControlContainer.VirtualPoint.X) / zoom), (int)((e.Location.Y + scrollingControlContainer.VirtualPoint.Y) / zoom));

            toolStripStatusLabelCoordinates.Text = string.Format("{0:N}, {1:N}", location.X, location.Y);

            KMLElementWithOffset resultElement;
            kmlFileManager.HitTest(location, out resultElement, !toolStripButtonAllowBackgroundSelection.Checked);
            if (resultElement != null && !selectedElements.Contains(resultElement))
            {
            }
            else if (selectedElements.Count > 0)
            {
                // Start the pan or resize of the selected elements (starting from the last selectedElement)
                KMLElementWithOffset lastSelectedElement = GetLastSelectedElement();
                if (lastSelectedElement != null)
                {
                    KMLElementWithOffset.SelectionPart selectionPart = lastSelectedElement.HitTestForSelection(location);
                    if (selectionPart != KMLElementWithOffset.SelectionPart.None)
                    {
                        draggingElement = lastSelectedElement;
                        draggingPart = selectionPart;
                        draggingStartLocation = e.Location;
                        foreach (var kmlElement in selectedElements)
                            kmlElement.BackupForDragging();
                        SetSelectionCursor(selectionPart);
                        return true; // Prevent default
                    }
                }
            }
            Cursor.Current = Cursors.Arrow;
            draggingElement = null;
            draggingPart = KMLElementWithOffset.SelectionPart.None;


            leftMouseButtonDownPulse = true;

            return true;
        }

        private bool ScrolledControl_MouseMove(object sender, MouseEventArgs e)
        {
            //Trace.WriteLine(string.Format("ScrolledControl_MouseMove"));

            if (e.Button == MouseButtons.Middle)
            {
                Cursor.Current = Cursors.NoMove2D;
                return false; // Allow default
            }

            if (leftMouseButtonDownPulse) {
                // Start the rectangular selection
                leftMouseButtonDownPulse = false;
                showSelectionRectangle = true;
                selectionRectangleStartLocation = e.Location;
                selectedElementsBeforeRectangleSelection.Clear();
                selectedElementsBeforeRectangleSelection.AddRange(selectedElements);
            }

            Point location = new Point((int)((e.Location.X + scrollingControlContainer.VirtualPoint.X) / zoom), (int)((e.Location.Y + scrollingControlContainer.VirtualPoint.Y) / zoom));

            toolStripStatusLabelCoordinates.Text = string.Format("{0},{1}", location.X, location.Y);

            int deltaX = e.Location.X - selectionRectangleStartLocation.X;
            int deltaY = e.Location.Y - selectionRectangleStartLocation.Y;

            if (showSelectionRectangle && deltaX != 0 && deltaY != 0)
            {
                // Update the rectangular selection
                selectionRectangleScaled = new Rectangle(
                    (int)((selectionRectangleStartLocation.X + scrollingControlContainer.VirtualPoint.X) / zoom),
                    (int)((selectionRectangleStartLocation.Y + scrollingControlContainer.VirtualPoint.Y) / zoom),
                    (int)(deltaX / zoom),
                    (int)(deltaY / zoom)
                );

                // If the rectangle has a negative size, make it positif
                if (selectionRectangleScaled.Width < 0)
                    selectionRectangleScaled.X -= selectionRectangleScaled.Width = -selectionRectangleScaled.Width;
                if (selectionRectangleScaled.Height < 0)
                    selectionRectangleScaled.Y -= selectionRectangleScaled.Height = -selectionRectangleScaled.Height;

                selectedElements.Clear();
                if ((ModifierKeys & Keys.Control) != Keys.Control)
                    // If the control key is not pushed, we start again a new selection
                    foreach (var kmlElement in kmlFileManager.GetElementsWithOffsetAndSize()) kmlElement.isSelected = false;
                else
                    selectedElements.AddRange(selectedElementsBeforeRectangleSelection);

                foreach (var element in kmlFileManager.GetElementsWithOffsetAndSize())
                {
                    if (!toolStripButtonAllowBackgroundSelection.Checked && element is KMLBackground)
                        continue;
                    if (selectionRectangleScaled.IntersectsWith(element.rectangle))
                    {
                        if (selectedElements.Contains(element))
                            selectedElements.Remove(element);
                        else
                            selectedElements.Add(element);
                    }
                }
                foreach (var element in kmlFileManager.GetElementsWithOffsetAndSize())
                    element.isSelected = selectedElements.Contains(element);

                InvalidateDisplay();

                toolStripStatusLabelCoordinates.Text = string.Format("{0},{1} {2}x{3}", selectionRectangleScaled.X, selectionRectangleScaled.Y, selectionRectangleScaled.Width, selectionRectangleScaled.Height);
            }
            else if (draggingElement != null)
            {
                // Pan or resize the selected elements
                int draggingDeltaX = (int)((e.Location.X - draggingStartLocation.X) / zoom);
                int draggingDeltaY = (int)((e.Location.Y - draggingStartLocation.Y) / zoom);
                bool isShiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
                foreach (var kmlElement in selectedElements)
                    kmlElement.UpdateForDragging(draggingDeltaX, draggingDeltaY, draggingPart, isShiftPressed, toolStripButtonAllowMoveDownPart.Checked);
                InvalidateDisplay();
                UpdatePropertyGridForSelectedElement();
            }
            else if (selectedElements.Count > 0)
            {
                // Change the cursor appearence following the KML element 
                KMLElementWithOffset lastSelectedElement = GetLastSelectedElement();
                if (lastSelectedElement != null)
                {
                    KMLElementWithOffset.SelectionPart selectionPart = lastSelectedElement.HitTestForSelection(location);
                    SetSelectionCursor(selectionPart);
                }
            }
            else
            {
                Cursor.Current = Cursors.Arrow;
            }
            return true; // Prevent default
        }

        private bool ScrolledControl_MouseUp(object sender, MouseEventArgs e)
        {
            //Trace.WriteLine(string.Format("ScrolledControl_MouseUp"));

            if (e.Button == MouseButtons.Middle)
                Cursor.Current = Cursors.Arrow;

            if (e.Button != MouseButtons.Left)
                return false;

            if (showSelectionRectangle)
            {
                // End of the rectangle selection
                showSelectionRectangle = false;
                KMLElement lastSelectedElement = GetLastSelectedElement();
                if (lastSelectedElement != null)
                    SelectSourceCodeFromKMLElement(lastSelectedElement);
                UpdatePropertyGridForSelectedElement();
                InvalidateDisplay();
            }
            else if (draggingElement != null && (ModifierKeys & Keys.Control) != Keys.Control)
            {
                // End modifying a KML element
            //    draggingElement = null;
            //    draggingPart = KMLElementWithOffset.SelectionPart.None;
            }
            else
            {
                // Mouse click event (Down and Up)

                Point location = new Point((int)((e.Location.X + scrollingControlContainer.VirtualPoint.X) / zoom), (int)((e.Location.Y + scrollingControlContainer.VirtualPoint.Y) / zoom));

                KMLElementWithOffset resultElement;
                kmlFileManager.HitTest(location, out resultElement, !toolStripButtonAllowBackgroundSelection.Checked);
                if (resultElement != null)
                {
                    // A KML element has been clicked, change the selection
                    if ((ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        // Add one KML element to the selection
                        if (resultElement.isSelected)
                        {
                            selectedElements.Remove(resultElement);
                            resultElement.isSelected = false;
                        }
                        else
                        {
                            selectedElements.Add(resultElement);
                            resultElement.isSelected = true;
                        }
                    }
                    else
                    {
                        // Select only one KML element
                        selectedElements.Clear();
                        selectedElements.Add(resultElement);
                        foreach (var kmlElement in kmlFileManager.GetElements()) kmlElement.isSelected = false;
                        resultElement.isSelected = true;
                    }
                }
                else
                {
                    selectedElements.Clear();
                    foreach (var kmlElement in kmlFileManager.GetElements()) kmlElement.isSelected = false;
                }
                SelectSourceCodeFromKMLElement(GetLastSelectedElement());
                UpdatePropertyGridForSelectedElement();
                InvalidateDisplay();
            }

            draggingElement = null;
            draggingPart = KMLElementWithOffset.SelectionPart.None;

            leftMouseButtonDownPulse = false;
            showSelectionRectangle = false;
            return true;
        }

        private void propertyGridSelectedElement_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            InvalidateDisplay();
        }

        private void InitializeForDisplay()
        {
            SuspendLayout();
            foreach (var kmlFile in kmlFileManager.GetFiles())
            {
                TextBox textBoxKMLFile = new TextBox();
                textBoxKMLFile.AcceptsReturn = true;
                textBoxKMLFile.AcceptsTab = true;
                textBoxKMLFile.AllowDrop = true;
                textBoxKMLFile.Dock = DockStyle.Fill;
                textBoxKMLFile.Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                textBoxKMLFile.Multiline = true;
                textBoxKMLFile.HideSelection = false;
                textBoxKMLFile.WordWrap = false;
                textBoxKMLFile.ScrollBars = ScrollBars.Both;
                textBoxKMLFile.TabIndex = 0;
                textBoxKMLFile.ReadOnly = true;
                textBoxKMLFile.Text = kmlFile.GetSourceCode();

                TabPage tabPageKMLFile = new TabPage();
                tabPageKMLFile.Controls.Add(textBoxKMLFile);
                tabPageKMLFile.Padding = new Padding(3);
                tabPageKMLFile.TabIndex = 0;
                tabPageKMLFile.Text = kmlFile.GetFilenameOnly();
                tabPageKMLFile.UseVisualStyleBackColor = true;

                this.tabControlKMLFiles.Controls.Add(tabPageKMLFile);

                UIElementForKMLFile uiElement = new UIElementForKMLFile();
                uiElement.tabPage = tabPageKMLFile;
                uiElement.textBox = textBoxKMLFile;
                uiElementPerKMLFile[kmlFile] = uiElement;
            }
            ResumeLayout(false);
            PerformLayout();

            //foreach (var item in uiElementPerKMLFile)
            //{
            //    if(item.Value.textBox != null)
            //    {
                    //var size = TextRenderer.MeasureText(textBox.Text, textBox.Font, textBox.ClientSize, TextFormatFlags.TextBoxControl);
                    //int lines = textBox.Lines.Length;
                    //int lineHeight = (size.Height / lines);

                    //// Value assigned to 'lines' does not reflect number of lines displayed:
                    //int lines = (textBox.Height / lineHeight);
            //    }
            //}

            bitmap = new Bitmap(kmlFileManager.RootBasePath + kmlFileManager.BitmapFilename);
            scrollingControlContainer.VirtualSize = new Size((int)(bitmap.Width * zoom), (int)(bitmap.Height * zoom));

            InvalidateDisplay();
        }

        private void Cleanup()
        {
            kmlFileManager.Cleanup();
            zoom = 1.0f;
            if (bitmap != null)
            {
                bitmap.Dispose();
                bitmap = null;
            }
            uiElementPerKMLFile.Clear();

            tabControlKMLFiles.Controls.Clear();
            selectedElements.Clear();
            UpdatePropertyGridForSelectedElement();
        }

        private KMLElementWithOffset GetLastSelectedElement()
        {
            return selectedElements.Count > 0 ? selectedElements.Last() : null;
        }

        private void SelectSourceCodeFromKMLElement(KMLElement resultElement)
        {
            if(resultElement != null && resultElement.elementLineNumber >= 0) {
                var uiElement = uiElementPerKMLFile[resultElement.kmlFile];
                if (uiElement.tabPage != null)
                    tabControlKMLFiles.SelectedTab = uiElement.tabPage;
                if (uiElement.textBox != null)
                {
                    try
                    {
                        uiElement.textBox.SelectionStart = uiElement.textBox.GetFirstCharIndexFromLine(resultElement.elementLineNumber + Utils.GetNumberOfVisibleLines(uiElement.textBox) / 2);
                        uiElement.textBox.ScrollToCaret();
                        uiElement.textBox.SelectionStart = uiElement.textBox.GetFirstCharIndexFromLine(resultElement.elementLineNumber);
                        uiElement.textBox.SelectionLength = uiElement.textBox.Lines[resultElement.elementLineNumber].Length;
                        uiElement.textBox.ScrollToCaret();
                    } catch (Exception) { }
                }
            }
        }

        private void UpdatePropertyGridForSelectedElement()
        {
            propertyGridSelectedElement.SelectedObjects = selectedElements.ToArray();
        }

        private static void SetSelectionCursor(KMLElementWithOffset.SelectionPart selectionPart)
        {
            switch (selectionPart)
            {
                case KMLElementWithOffset.SelectionPart.Inside:
                    Cursor.Current = Cursors.SizeAll;
                    break;
                case KMLElementWithOffset.SelectionPart.TopLeft:
                    Cursor.Current = Cursors.SizeNWSE;
                    break;
                case KMLElementWithOffset.SelectionPart.Top:
                    Cursor.Current = Cursors.SizeNS;
                    break;
                case KMLElementWithOffset.SelectionPart.TopRight:
                    Cursor.Current = Cursors.SizeNESW;
                    break;
                case KMLElementWithOffset.SelectionPart.Right:
                    Cursor.Current = Cursors.SizeWE;
                    break;
                case KMLElementWithOffset.SelectionPart.BottomRight:
                    Cursor.Current = Cursors.SizeNWSE;
                    break;
                case KMLElementWithOffset.SelectionPart.Bottom:
                    Cursor.Current = Cursors.SizeNS;
                    break;
                case KMLElementWithOffset.SelectionPart.BottomLeft:
                    Cursor.Current = Cursors.SizeNESW;
                    break;
                case KMLElementWithOffset.SelectionPart.Left:
                    Cursor.Current = Cursors.SizeWE;
                    break;
                default:
                    Cursor.Current = Cursors.Arrow;
                    break;
            }
        }

        private void InvalidateDisplay()
        {
            this.scrollingControlContainer.ScrolledControl.Invalidate();
        }
    }
}
