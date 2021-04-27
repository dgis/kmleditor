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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KMLEditor
{
    public partial class KMLEditorForm : Form
    {
        private KMLFileManager kmlFileManager = new KMLFileManager();
        private UndoManager undoManager = new UndoManager();
        private float zoom = 1.0f;
        private static float zoomFactor = 1.25f;
        private float maxZoom = (float)Math.Pow(zoomFactor, 21);
        private float minZoom = (float)Math.Pow(zoomFactor, -7);

        private Bitmap bitmap;
        class UIElementForKMLFile {
            public TabPage tabPage;
            public TextBox textBox;
        };
        private Dictionary<KMLFile, UIElementForKMLFile> uiElementPerKMLFile = new Dictionary<KMLFile, UIElementForKMLFile>();
        private List<KMLElementWithOffset> selectedElements = new List<KMLElementWithOffset>();
        private List<KMLElementWithOffset> selectedElementsBeforeRectangleSelection = new List<KMLElementWithOffset>();
        private MultipleSelection multipleSelection = new MultipleSelection();

        private KMLElementWithOffset draggingElement = null;
        private KMLElementWithOffset.SelectionPart draggingPart = KMLElementWithOffset.SelectionPart.None;
        private Point draggingStartLocation;
        private List<KMLElementWithOffset> draggingSelectedElements = new List<KMLElementWithOffset>();
        private MultipleSelection.SelectionPart draggingMultipleSelectionPart = MultipleSelection.SelectionPart.None;

        private static SolidBrush brushForeground = new SolidBrush(Color.DarkGoldenrod);
        private static Pen penForeground = new Pen(brushForeground);
        private static Pen penForegroundButtonDown = new Pen(brushForeground);
        private static SolidBrush brushForegroundSelected = new SolidBrush(Color.Red);
        private static SolidBrush brushForegroundButtonSelected = new SolidBrush(Color.Pink);
        private static SolidBrush brushSelectionColor = new SolidBrush(Color.Gray);
        private static Pen penForegroundSelected = new Pen(brushForegroundSelected);
        private static Pen penForegroundButtonSelected = new Pen(brushForegroundButtonSelected);
        private static Pen penForegroundButtonDownSelected = new Pen(brushForegroundSelected);
        private static Pen penRectangularSelection = new Pen(brushSelectionColor);
        private static Pen penRectangularSelectionForResize = new Pen(brushSelectionColor);
        private Point pointSelectionStartLocation = new Point();
        private RectangleF rectangleScaledSelection = new RectangleF();
        private bool showSelectionRectangle = false;
        private bool leftMouseButtonDownPulse = false;

        public KMLEditorForm()
        {
            InitializeComponent();

            UpdateUndoMenuAndToolbar();

            scrollingControlContainer.VirtualSize = new Size(0, 0);
            scrollingControlContainer.ContentMouseWheel += ScrolledControl_MouseWheel;
            scrollingControlContainer.ContentMouseDown += ScrolledControl_MouseDown;
            scrollingControlContainer.ContentMouseMove += ScrolledControl_MouseMove;
            scrollingControlContainer.ContentMouseUp += ScrolledControl_MouseUp;
            scrollingControlContainer.ScrolledControl.Paint += ScrolledControl_Paint;

            penRectangularSelection.DashStyle = DashStyle.Dash;
            penForegroundButtonDown.DashStyle = DashStyle.Dash;
            penForegroundButtonDownSelected.DashStyle = DashStyle.Dash;

            toolStripComboBoxDownPart.SelectedIndex = 0;

            UpdateZoomStatus();
        }

        private void KMLEditorForm_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
                OpenFile(args[1]);
        }

        private void KMLEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if DEBUG
            return;
#endif
            foreach (var kmlFile in kmlFileManager.GetFiles())
                if (kmlFile.isDirty)
                    if (MessageBox.Show("At least one file is not saved. Quit anyway?", "", MessageBoxButtons.YesNo) == DialogResult.No)
                        e.Cancel = true;
        }

        private void KMLEditorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cleanup();
        }

        private void KMLEditorForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("UniformResourceLocator"))
            {
                string url = e.Data.GetData(DataFormats.Text, true) as string;
                if (!string.IsNullOrEmpty(url))
                    OpenFile(url.Trim());
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                    OpenFile(files[0]);
            }
        }
        private void KMLEditorForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)
                || e.Data.GetDataPresent("UniformResourceLocator")
                )
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "kml files (*.kml)|*.kml|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                OpenFile(openFileDialog.FileName);
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KMLFile rootKMLFile = kmlFileManager.GetRootFile();
            if(rootKMLFile != null)
                OpenFile(rootKMLFile.GetFilename());
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

        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            List<UndoManager.DoOperation> doOperations = undoManager.Undo();
            if(doOperations != null)
            {
                foreach (var doOperation in doOperations)
                {
                    if (doOperation.Before is KMLElementWithOffset)
                    {
                        KMLElementWithOffset currentElement = kmlFileManager.GetElementWithOffsetById(doOperation.Before.id);
                        if (currentElement != null)
                        {
                            // Modify back to its previous state
                            currentElement.CopyFrom(doOperation.Before);
                        }
                        else
                        {
                            // The element has been remove, put it back
                        }
                    }
                }
                UpdateAfterDoOperation();
            }
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            List<UndoManager.DoOperation> doOperations = undoManager.Redo();
            if (doOperations != null)
            {
                foreach (var doOperation in doOperations)
                {
                    if (doOperation.After is KMLElementWithOffset)
                    {
                        KMLElementWithOffset currentElement = kmlFileManager.GetElementWithOffsetById(doOperation.After.id);
                        if (currentElement != null)
                        {
                            // Modify back to its previous state
                            currentElement.CopyFrom(doOperation.After);
                        }
                        else
                        {
                            // The element has been remove, put it back
                        }
                    }
                }
                UpdateAfterDoOperation();
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var kMLElementWithOffset in kmlFileManager.GetElementsWithOffsetAndSize())
            {
                if (!toolStripButtonAllowBackgroundSelection.Checked && kMLElementWithOffset is KMLBackground)
                    continue;

                kMLElementWithOffset.isSelected = true;
            }
            UpdateSelectedElementsList();
            UpdatePropertyGridAndListBoxForSelectedElement();
            UpdateRectangleWithSelectedElements();
            InvalidateDisplay();
            UpdateKMLSource();
        }


        private void toolStripButtonUpdateKMLSource_Click(object sender, EventArgs e)
        {
            UpdateKMLSource();
        }

        private void toolStripButtonHelp_Click(object sender, EventArgs e)
        {
            new AboutForm().ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ScrolledControl_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            Point virtualPoint = scrollingControlContainer.VirtualPoint;
            e.Graphics.TranslateTransform(-virtualPoint.X, -virtualPoint.Y);
            e.Graphics.ScaleTransform(zoom, zoom);
            e.Graphics.SmoothingMode = SmoothingMode.None;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            if(zoom > 1.0f)
            {
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            }

            if (bitmap != null)
                graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            float zoomInverse = 1.0f / zoom;
            penForeground.Width = zoomInverse;
            penForegroundButtonDown.Width = zoomInverse;
            Pen pen = penForeground;
            foreach (var element in kmlFileManager.GetElementsWithOffsetAndSize())
            {
                RectangleF rectangle = element.GetBoundForPart(KMLElementWithOffset.SelectionPart.Element, zoom);
                if (element is KMLLcd)
                {
                    graphics.DrawLine(pen, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Top);
                    graphics.DrawLine(pen, rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom);
                }
                else
                    graphics.DrawRectangle(pen, rectangle);

                if (element is KMLElementWithOffsetAndSizeAndDown)
                {
                    KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)element;
                    if (kmlElementWithOffsetAndSizeAndDown.DownX != null && kmlElementWithOffsetAndSizeAndDown.DownY != null)
                        graphics.DrawRectangle(penForegroundButtonDown, new RectangleF((int)kmlElementWithOffsetAndSizeAndDown.DownX, (int)kmlElementWithOffsetAndSizeAndDown.DownY,
                                kmlElementWithOffsetAndSizeAndDown.SizeWidth, kmlElementWithOffsetAndSizeAndDown.SizeHeight));
                }
            }

            // Display selected elements above.
            penForegroundButtonSelected.Width = zoomInverse;
            penForegroundSelected.Width = zoomInverse;
            penForegroundButtonDownSelected.Width = zoomInverse;
            pen = penForegroundSelected;
            KMLElementWithOffset lastSelectedElement = GetLastSelectedElement();
            foreach (var element in kmlFileManager.GetElementsWithOffsetAndSize())
            {
                if (!element.isSelected)
                    continue;

                RectangleF rectangle = element.GetBoundForPart(KMLElementWithOffset.SelectionPart.Element, zoom);
                if (element == lastSelectedElement)
                {
                    if (element is KMLLcd)
                    {
                        graphics.DrawLine(penForegroundButtonSelected, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Top);
                        graphics.DrawLine(penForegroundButtonSelected, rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom);
                    }
                    else
                    {
                        graphics.DrawRectangle(penForegroundButtonSelected, rectangle);
                        graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.TopLeft, zoom));
                        graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.TopRight, zoom));
                        graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.BottomRight, zoom));
                        graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.BottomLeft, zoom));
                        if (element is KMLElementWithOffsetAndSizeAndDown)
                        {
                            KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)element;
                            if (kmlElementWithOffsetAndSizeAndDown.DownX != null && kmlElementWithOffsetAndSizeAndDown.DownY != null)
                                graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.InsideDown, zoom));
                        }
                    }
                    graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.Inside, zoom));
                }
                else if (element is KMLLcd)
                {
                    graphics.DrawLine(pen, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Top);
                    graphics.DrawLine(pen, rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom);
                }
                else
                    graphics.DrawRectangle(pen, rectangle);

                if (element is KMLElementWithOffsetAndSizeAndDown)
                {
                    KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)element;
                    if (kmlElementWithOffsetAndSizeAndDown.DownX != null && kmlElementWithOffsetAndSizeAndDown.DownY != null)
                        graphics.DrawRectangle(penForegroundButtonDownSelected, new RectangleF((int)kmlElementWithOffsetAndSizeAndDown.DownX, (int)kmlElementWithOffsetAndSizeAndDown.DownY,
                                    kmlElementWithOffsetAndSizeAndDown.SizeWidth, kmlElementWithOffsetAndSizeAndDown.SizeHeight));
                }
            }

            if (selectedElements.Count > 1 && multipleSelection.rectangle.Width > 0f && multipleSelection.rectangle.Height > 0f)
            {
                penRectangularSelectionForResize.Width = zoomInverse;
                graphics.DrawRectangle(penRectangularSelectionForResize, multipleSelection.GetBoundForPart(MultipleSelection.SelectionPart.TopLeft, zoom));
                graphics.DrawRectangle(penRectangularSelectionForResize, multipleSelection.GetBoundForPart(MultipleSelection.SelectionPart.Top, zoom));
                graphics.DrawRectangle(penRectangularSelectionForResize, multipleSelection.GetBoundForPart(MultipleSelection.SelectionPart.TopRight, zoom));
                graphics.DrawRectangle(penRectangularSelectionForResize, multipleSelection.GetBoundForPart(MultipleSelection.SelectionPart.Right, zoom));
                graphics.DrawRectangle(penRectangularSelectionForResize, multipleSelection.GetBoundForPart(MultipleSelection.SelectionPart.BottomRight, zoom));
                graphics.DrawRectangle(penRectangularSelectionForResize, multipleSelection.GetBoundForPart(MultipleSelection.SelectionPart.Bottom, zoom));
                graphics.DrawRectangle(penRectangularSelectionForResize, multipleSelection.GetBoundForPart(MultipleSelection.SelectionPart.BottomLeft, zoom));
                graphics.DrawRectangle(penRectangularSelectionForResize, multipleSelection.GetBoundForPart(MultipleSelection.SelectionPart.Left, zoom));
            }

            if (showSelectionRectangle)
            {
                penRectangularSelection.Width = zoomInverse;
                graphics.DrawRectangle(penRectangularSelection, rectangleScaledSelection);
            }
        }


        private bool ScrolledControl_MouseWheel(object sender, MouseEventArgs e)
        {
            float previousZoom = zoom;

            if (e.Delta > 0)
            {
                zoom *= zoomFactor;
                if (zoom > maxZoom)
                    zoom = maxZoom;
            }
            else
            {
                zoom /= zoomFactor;
                if (zoom < minZoom)
                    zoom = minZoom;
            }

            //e.Location -> is scrollingControlContainer based coords with no zoom
            //this.scrollingControlContainer.VirtualPoint -> is scrollingControlContainer based coords with no zoom
            scrollingControlContainer.VirtualSize = new Size((int)(bitmap.Width * zoom), (int)(bitmap.Height * zoom));
            this.scrollingControlContainer.ScrollTo(new Point(
                (int)((float)(zoom / previousZoom) * (e.Location.X + scrollingControlContainer.VirtualPoint.X) - e.Location.X),
                (int)((float)(zoom / previousZoom) * (e.Location.Y + scrollingControlContainer.VirtualPoint.Y) - e.Location.Y)));

            UpdateZoomStatus();
            InvalidateDisplay();

            return true; // Prevent default
        }

        private bool ScrolledControl_MouseDown(object sender, MouseEventArgs e)
        {
            //Trace.WriteLine(string.Format("ScrolledControl_MouseDown"));

            if (e.Button == MouseButtons.Middle)
            {
                Cursor.Current = Cursors.NoMove2D;
                return false; // Allow default
            }
            if (e.Button != MouseButtons.Left)
                return false;

            Point location = new Point((int)((e.Location.X + scrollingControlContainer.VirtualPoint.X) / zoom), (int)((e.Location.Y + scrollingControlContainer.VirtualPoint.Y) / zoom));

            toolStripStatusLabelCoordinates.Text = string.Format("{0},{1}", location.X, location.Y);

            if (selectedElements.Count > 0)
            {
                // Start the pan or resize of the selected elements (starting from the last selectedElement)
                KMLElementWithOffset lastSelectedElement = GetLastSelectedElement();
                if (lastSelectedElement != null)
                {
                    KMLElementWithOffset.SelectionPart selectionPart = lastSelectedElement.HitTestForSelection(location, zoom);
                    if (selectionPart != KMLElementWithOffset.SelectionPart.None)
                    {
                        draggingElement = lastSelectedElement;
                        draggingPart = selectionPart;
                        draggingStartLocation = e.Location;
                        draggingSelectedElements.Clear();
                        foreach (var kmlElement in selectedElements)
                        {
                            kmlElement.BackupBeforeDragging();
                            // Deep copy for undo
                            KMLElementWithOffset kmlElementWithOffsetClone = kmlElement.Clone() as KMLElementWithOffset;
                            if(kmlElementWithOffsetClone != null)
                                draggingSelectedElements.Add(kmlElementWithOffsetClone);
                        }
                        SetSelectionCursor(selectionPart);
                        return true; // Prevent default
                    }
                }

                if (selectedElements.Count > 1)
                {
                    // Start the resize of the multiple selected elements (rectangle containing all the selected elements)
                    MultipleSelection.SelectionPart multipleSelectionPart = multipleSelection.HitTestForSelection(location, zoom);
                    if (multipleSelectionPart != MultipleSelection.SelectionPart.None)
                    {
                        draggingMultipleSelectionPart = multipleSelectionPart;
                        draggingStartLocation = e.Location;
                        draggingSelectedElements.Clear();
                        multipleSelection.BackupBeforeDragging();
                        foreach (var kmlElement in selectedElements)
                        {
                            kmlElement.BackupBeforeDragging();
                            // Deep copy for undo
                            KMLElementWithOffset kmlElementWithOffsetClone = kmlElement.Clone() as KMLElementWithOffset;
                            if (kmlElementWithOffsetClone != null)
                                draggingSelectedElements.Add(kmlElementWithOffsetClone);
                        }
                        SetMultipleSelectionCursor(multipleSelectionPart);
                        return true; // Prevent default
                    }
                }
            }
            //KMLElementWithOffset resultElement;
            //kmlFileManager.HitTest(location, out resultElement, !toolStripButtonAllowBackgroundSelection.Checked);
            //if (resultElement != null && !selectedElements.Contains(resultElement))
            //{
            //}
            Cursor.Current = Cursors.Arrow;
            draggingElement = null;
            draggingPart = KMLElementWithOffset.SelectionPart.None;
            draggingMultipleSelectionPart = MultipleSelection.SelectionPart.None;

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
                pointSelectionStartLocation = e.Location;
                selectedElementsBeforeRectangleSelection.Clear();
                selectedElementsBeforeRectangleSelection.AddRange(selectedElements);
            }

            Point location = new Point((int)((e.Location.X + scrollingControlContainer.VirtualPoint.X) / zoom), (int)((e.Location.Y + scrollingControlContainer.VirtualPoint.Y) / zoom));

            toolStripStatusLabelCoordinates.Text = string.Format("{0},{1}", location.X, location.Y);

            int deltaX = e.Location.X - pointSelectionStartLocation.X;
            int deltaY = e.Location.Y - pointSelectionStartLocation.Y;

            bool isShiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
            bool isControlPressed = (ModifierKeys & Keys.Control) == Keys.Control;

            if (showSelectionRectangle && deltaX != 0 && deltaY != 0)
            {
                // Update the rectangular selection
                rectangleScaledSelection = new RectangleF(
                    (pointSelectionStartLocation.X + scrollingControlContainer.VirtualPoint.X) / zoom,
                    (pointSelectionStartLocation.Y + scrollingControlContainer.VirtualPoint.Y) / zoom,
                    deltaX / zoom,
                    deltaY / zoom
                );

                // If the rectangle has a negative size, make it positif
                if (rectangleScaledSelection.Width < 0)
                    rectangleScaledSelection.X -= rectangleScaledSelection.Width = -rectangleScaledSelection.Width;
                if (rectangleScaledSelection.Height < 0)
                    rectangleScaledSelection.Y -= rectangleScaledSelection.Height = -rectangleScaledSelection.Height;

                selectedElements.Clear();
                if (isControlPressed)
                    selectedElements.AddRange(selectedElementsBeforeRectangleSelection);
                else
                    // If the control key is not pushed, we start again a new selection
                    foreach (var kmlElement in kmlFileManager.GetElementsWithOffsetAndSize())
                        kmlElement.isSelected = false;

                foreach (var element in kmlFileManager.GetElementsWithOffsetAndSize())
                {
                    if (!toolStripButtonAllowBackgroundSelection.Checked && element is KMLBackground)
                        continue;
                    if (rectangleScaledSelection.IntersectsWith(element.GetBoundForPart(KMLElementWithOffset.SelectionPart.Element, zoom)))
                    {
                        if (selectedElements.Contains(element))
                            selectedElements.Remove(element);
                        else
                            selectedElements.Add(element);
                    }
                }
                UpdateSelectedElements();
                UpdateRectangleWithSelectedElements();

                InvalidateDisplay();

                toolStripStatusLabelCoordinates.Text = string.Format("{0},{1} {2}x{3}", rectangleScaledSelection.X, rectangleScaledSelection.Y, rectangleScaledSelection.Width, rectangleScaledSelection.Height);
            }
            else if (draggingElement != null)
            {
                // Pan or resize the selected elements
                Point draggingDelta = new Point((int)((e.Location.X - draggingStartLocation.X) / zoom), (int)((e.Location.Y - draggingStartLocation.Y) / zoom));
                foreach (var kmlElement in selectedElements)
                    kmlElement.ModifyRectangle(draggingDelta, draggingPart, isShiftPressed, isControlPressed, toolStripButtonAllowMoveDownPart.Checked);

                UpdateRectangleWithSelectedElements();
                UpdatePropertyGridForSelectedElement();
                InvalidateDisplay();

                toolStripStatusLabelCoordinates.Text = string.Format("{0},{1} Delta:{2}x{3}", draggingElement.OffsetX, draggingElement.OffsetY, draggingDelta.X, draggingDelta.Y);
            }
            else if (draggingMultipleSelectionPart != MultipleSelection.SelectionPart.None)
            {
                // Resize a multiple selected elements at once (rectangle containing all the selected elements)
                PointF draggingDelta = new PointF((e.Location.X - draggingStartLocation.X) / zoom, (e.Location.Y - draggingStartLocation.Y) / zoom);
                RectangleF newRectangle = multipleSelection.ModifyRectangle(draggingDelta, draggingMultipleSelectionPart, isShiftPressed, isControlPressed);
                foreach (var kmlElement in selectedElements)
                    kmlElement.Resize(multipleSelection.dragRectangle, newRectangle, toolStripButtonAllowMoveDownPart.Checked);
                UpdateRectangleWithSelectedElements();
                UpdatePropertyGridForSelectedElement();
                InvalidateDisplay();

                toolStripStatusLabelCoordinates.Text = string.Format("{0},{1} {2}x{3} Delta:{4}x{5} Delta:{6}%x{7}%",
                    (int)newRectangle.X, (int)newRectangle.Y, (int)newRectangle.Width, (int)newRectangle.Height,
                    (int)draggingDelta.X, (int)draggingDelta.Y,
                    (int)(newRectangle.Width / multipleSelection.dragRectangle.Width * 100f), (int)(newRectangle.Height / multipleSelection.dragRectangle.Height * 100f));
            }
            else if (selectedElements.Count > 0)
            {
                // Change the cursor appearence following the KML element 
                bool cursorChanged = false;
                KMLElementWithOffset lastSelectedElement = GetLastSelectedElement();
                if (lastSelectedElement != null)
                    cursorChanged = SetSelectionCursor(lastSelectedElement.HitTestForSelection(location, zoom));
                if(!cursorChanged)
                    SetMultipleSelectionCursor(multipleSelection.HitTestForSelection(location, zoom));
            }
            else
                Cursor.Current = Cursors.Arrow;
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
                UpdatePropertyGridAndListBoxForSelectedElement();
                UpdateRectangleWithSelectedElements();
                InvalidateDisplay();
            }
            else if ((draggingElement != null)
                || draggingMultipleSelectionPart != MultipleSelection.SelectionPart.None)
            {
                // End modifying a KML element
                if (draggingSelectedElements.Count > 0)
                {
                    List<UndoManager.DoOperation> operations = new List<UndoManager.DoOperation>();
                    foreach (var kmlElementWithOffset in draggingSelectedElements)
                    {
                        operations.Add(new UndoManager.DoOperation
                        {
                            Before = kmlElementWithOffset,
                            After = kmlFileManager.GetElementWithOffsetById(kmlElementWithOffset.id).Clone(),
                            OperationType = UndoManager.OperationType.PanOrResize,
                            ModifiedPart = draggingPart
                        });
                    }
                    undoManager.Modify(operations);
                    UpdatePropertyGridAndListBoxForSelectedElement();
                    UpdateRectangleWithSelectedElements();
                    UpdateUndoMenuAndToolbar();
                    UpdateKMLSource();
                    InvalidateDisplay();
                }
                draggingElement = null;
                draggingPart = KMLElementWithOffset.SelectionPart.None;
                draggingMultipleSelectionPart = MultipleSelection.SelectionPart.None;
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
                UpdatePropertyGridAndListBoxForSelectedElement();
                UpdateRectangleWithSelectedElements();
                InvalidateDisplay();
            }

            draggingElement = null;
            draggingPart = KMLElementWithOffset.SelectionPart.None;
            draggingMultipleSelectionPart = MultipleSelection.SelectionPart.None;

            leftMouseButtonDownPulse = false;
            showSelectionRectangle = false;
            return true;
        }

        private void listBoxSelectedElement_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (preventListBoxSelectedElementEvent)
                return;
            // Add new selected element trying to preserve the user selection order
            foreach (var kmlElementWithOffset in listBoxSelectedElement.SelectedItems)
                if (!selectedElements.Contains(kmlElementWithOffset) && kmlElementWithOffset is KMLElementWithOffset)
                    selectedElements.Add((KMLElementWithOffset)kmlElementWithOffset);
            foreach (var kmlElementWithOffset in selectedElements.ToList())
                if (!listBoxSelectedElement.SelectedItems.Contains(kmlElementWithOffset))
                    selectedElements.Remove(kmlElementWithOffset);
            UpdateSelectedElements();
            SelectSourceCodeFromKMLElement(GetLastSelectedElement());
            UpdatePropertyGridForSelectedElement();
            UpdateRectangleWithSelectedElements();
            InvalidateDisplay();
        }

        private void propertyGridSelectedElement_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            string property = e.ChangedItem.Label;
            int? oldValue = (int)e.OldValue;
            List<UndoManager.DoOperation> operations = new List<UndoManager.DoOperation>();
            foreach (var kmlElement in propertyGridSelectedElement.SelectedObjects)
            {
                if(kmlElement is KMLElementWithOffset)
                {
                    KMLElementWithOffset kmlElementWithOffset = (KMLElementWithOffset)kmlElement;
                    UndoManager.DoOperation operation = new UndoManager.DoOperation
                    {
                        Before = kmlElementWithOffset.Clone(),
                        After = kmlElementWithOffset.Clone(),
                        OperationType = UndoManager.OperationType.PanOrResize,
                        ModifiedPart = draggingPart
                    };
                    switch (property)
                    {
                        case "OffsetX":
                            if(oldValue != null && kmlElement is KMLElementWithOffset)
                            {
                                ((KMLElementWithOffset)operation.Before).OffsetX = (int)oldValue;
                                operation.OperationType = UndoManager.OperationType.PanOrResize;
                                operation.ModifiedPart = KMLElementWithOffset.SelectionPart.Left;
                            }
                            break;
                        case "OffsetY":
                            if (oldValue != null && kmlElement is KMLElementWithOffset)
                            {
                                ((KMLElementWithOffset)operation.Before).OffsetY = (int)oldValue;
                                operation.OperationType = UndoManager.OperationType.PanOrResize;
                                operation.ModifiedPart = KMLElementWithOffset.SelectionPart.Top;
                            }
                            break;
                        case "SizeWidth":
                            if (oldValue != null && kmlElement is KMLElementWithOffsetAndSize)
                            {
                                ((KMLElementWithOffsetAndSize)operation.Before).SizeWidth = (int)oldValue;
                                operation.OperationType = UndoManager.OperationType.PanOrResize;
                                operation.ModifiedPart = KMLElementWithOffset.SelectionPart.Right;
                            }
                            break;
                        case "SizeHeight":
                            if (oldValue != null && kmlElement is KMLElementWithOffsetAndSize)
                            {
                                ((KMLElementWithOffsetAndSize)operation.Before).SizeHeight = (int)oldValue;
                                operation.OperationType = UndoManager.OperationType.PanOrResize;
                                operation.ModifiedPart = KMLElementWithOffset.SelectionPart.Bottom;
                            }
                            break;
                        case "DownX":
                            if (kmlElement is KMLElementWithOffsetAndSizeAndDown) {
                                ((KMLElementWithOffsetAndSizeAndDown)operation.Before).DownX = oldValue;
                            }
                            break;
                        case "DownY":
                            if (kmlElement is KMLElementWithOffsetAndSizeAndDown) {
                                ((KMLElementWithOffsetAndSizeAndDown)operation.Before).DownY = oldValue;
                            }
                            break;
                        case "Number":
                            if (oldValue != null)
                            {
                                if (kmlElement is KMLButton)
                                {
                                    ((KMLButton)operation.Before).Number = (int)oldValue;
                                }
                                else if (kmlElement is KMLAnnunciator)
                                {
                                    ((KMLAnnunciator)operation.Before).Number = (int)oldValue;
                                }
                            }
                            break;
                        case "Type":
                            if (oldValue != null && kmlElement is KMLButton)
                            {
                                ((KMLButton)operation.Before).Type = (int)oldValue;
                            }
                            break;
                    }
                    operations.Add(operation);
                }
            }
            if(operations.Count > 0)
                undoManager.Modify(operations);
            UpdateUndoMenuAndToolbar();
            UpdateRectangleWithSelectedElements();
            InvalidateDisplay();
            UpdateKMLSource();
        }

        private void OpenFile(string rootKMLFilename)
        {
            if (File.Exists(rootKMLFilename))
            {
                Cleanup();
                if (kmlFileManager.AddKMLFile(rootKMLFilename))
                    InitializeForDisplay();
            }
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

            listBoxSelectedElement.BeginUpdate();
            listBoxSelectedElement.Items.Clear();
            foreach (var kmlElement in kmlFileManager.GetElementsWithOffsetAndSize())
                listBoxSelectedElement.Items.Add(kmlElement);
            listBoxSelectedElement.EndUpdate();

            ResumeLayout(false);
            PerformLayout();

            if(kmlFileManager.BitmapFilename != null)
            {
                bitmap = new Bitmap(kmlFileManager.RootBasePath + kmlFileManager.BitmapFilename);
                scrollingControlContainer.VirtualSize = new Size((int)(bitmap.Width * zoom), (int)(bitmap.Height * zoom));
            }

            InvalidateDisplay();
        }

        private void Cleanup()
        {
            kmlFileManager.Cleanup();
            zoom = 1.0f;
            scrollingControlContainer.VirtualPoint = new Point(0, 0);
            scrollingControlContainer.VirtualSize = new Size(10, 10);
            if (bitmap != null)
            {
                bitmap.Dispose();
                bitmap = null;
            }
            uiElementPerKMLFile.Clear();

            tabControlKMLFiles.Controls.Clear();
            selectedElements.Clear();
            UpdatePropertyGridAndListBoxForSelectedElement();
            UpdateRectangleWithSelectedElements();
        }

        private void UpdateAfterDoOperation()
        {
            UpdateSelectedElementsList();
            UpdatePropertyGridAndListBoxForSelectedElement();
            UpdateRectangleWithSelectedElements();
            UpdateUndoMenuAndToolbar();
            InvalidateDisplay();
            UpdateKMLSource();
        }

        private void UpdateSelectedElements()
        {
            foreach (var element in kmlFileManager.GetElementsWithOffsetAndSize())
                element.isSelected = selectedElements.Contains(element);
        }

        private void UpdateSelectedElementsList()
        {
            selectedElements.Clear();
            selectedElements.AddRange(kmlFileManager.GetElementsWithOffsetAndSize().Where(element => element.isSelected));
        }

        private void UpdateRectangleWithSelectedElements()
        {
            multipleSelection.rectangle = new RectangleF();
            if(selectedElements.Count > 1)
            {
                multipleSelection.rectangle = selectedElements[0].GetBoundForPart(KMLElementWithOffset.SelectionPart.Element, zoom);
                for (int i = 1; i < selectedElements.Count; i++)
                {
                    var selectedElement = selectedElements[i];
                    multipleSelection.rectangle = RectangleF.Union(multipleSelection.rectangle, selectedElement.GetBoundForPart(KMLElementWithOffset.SelectionPart.Element, zoom));
                }
            }
        }

        private void UpdateUndoMenuAndToolbar()
        {
            undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = undoManager.CanUndo();
            redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = undoManager.CanRedo();
        }
        private void UpdateZoomStatus()
        {
            toolStripStatusLabelZoom.Text = string.Format("{0}%", (int)Math.Round(zoom * 100.0f));
        }

        private void UpdateSourceCodeTabPageTitle(KMLFile kmlFile)
        {
            UIElementForKMLFile uiElement = uiElementPerKMLFile[kmlFile];
            if (uiElement != null)
            {
                TabPage tabPage = uiElement.tabPage;
                tabPage.Text = kmlFile.GetFilenameOnly() + (kmlFile.isDirty ? "*" : "");
            }
        }

        private void UpdateKMLSource()
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

        private KMLElementWithOffset GetLastSelectedElement()
        {
            return selectedElements.Count > 0 ? selectedElements.Last() : null;
        }
        private void SetLastSelectedElement(KMLElementWithOffset kmlElementWithOffset)
        {
            if(kmlElementWithOffset != null)
            {
                if (selectedElements.Contains(kmlElementWithOffset))
                    selectedElements.Remove(kmlElementWithOffset);
                selectedElements.Add(kmlElementWithOffset);
            }
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

        private void UpdatePropertyGridAndListBoxForSelectedElement()
        {
            UpdatePropertyGridForSelectedElement();
            UpdateListBoxForSelectedElement();
        }

        private void UpdatePropertyGridForSelectedElement()
        {
            propertyGridSelectedElement.SelectedObjects = selectedElements.ToArray();
        }
        bool preventListBoxSelectedElementEvent = false;
        private void UpdateListBoxForSelectedElement()
        {
            preventListBoxSelectedElementEvent = true;
            listBoxSelectedElement.BeginUpdate();
            listBoxSelectedElement.SelectedItems.Clear();
            foreach (var selectedElement in selectedElements.ToList())
                listBoxSelectedElement.SelectedItems.Add(selectedElement);
            listBoxSelectedElement.EndUpdate();
            preventListBoxSelectedElementEvent = false;
        }

        private bool SetSelectionCursor(KMLElementWithOffset.SelectionPart selectionPart)
        {
            // http://www.authorcode.com/show-all-cursors/
            // https://www.oreilly.com/library/view/visual-basic-2012/9781118332085/xhtml/sec65.html
            const string help = "Resize the {0} of each element selected (Shift for centering, Ctrl to keep the ratio).";
            switch (selectionPart)
            {
                case KMLElementWithOffset.SelectionPart.Inside:
                    Cursor.Current = Cursors.SizeAll;
                    toolStripStatusLabelHelp.Text = "Move each selected element.";
                    return true;

                case KMLElementWithOffset.SelectionPart.InsideDown:
                    Cursor.Current = Cursors.SizeAll;
                    toolStripStatusLabelHelp.Text = "Move of the Down part of each element selected.";
                    return true;

                case KMLElementWithOffset.SelectionPart.TopLeft:
                    Cursor.Current = Cursors.SizeNWSE;
                    toolStripStatusLabelHelp.Text = string.Format(help, "top/left");
                    return true;

                case KMLElementWithOffset.SelectionPart.Top:
                    Cursor.Current = Cursors.SizeNS;
                    toolStripStatusLabelHelp.Text = string.Format(help, "top");
                    return true;

                case KMLElementWithOffset.SelectionPart.TopRight:
                    Cursor.Current = Cursors.SizeNESW;
                    toolStripStatusLabelHelp.Text = string.Format(help, "top/right");
                    return true;

                case KMLElementWithOffset.SelectionPart.Right:
                    Cursor.Current = Cursors.SizeWE;
                    toolStripStatusLabelHelp.Text = string.Format(help, "right");
                    return true;

                case KMLElementWithOffset.SelectionPart.BottomRight:
                    Cursor.Current = Cursors.SizeNWSE;
                    toolStripStatusLabelHelp.Text = string.Format(help, "bottom/right");
                    return true;

                case KMLElementWithOffset.SelectionPart.Bottom:
                    Cursor.Current = Cursors.SizeNS;
                    toolStripStatusLabelHelp.Text = string.Format(help, "bottom");
                    return true;

                case KMLElementWithOffset.SelectionPart.BottomLeft:
                    Cursor.Current = Cursors.SizeNESW;
                    toolStripStatusLabelHelp.Text = string.Format(help, "bottom/left");
                    return true;

                case KMLElementWithOffset.SelectionPart.Left:
                    Cursor.Current = Cursors.SizeWE;
                    toolStripStatusLabelHelp.Text = string.Format(help, "left");
                    return true;

                default:
                    Cursor.Current = Cursors.Arrow;
                    toolStripStatusLabelHelp.Text = "";
                    return false;
            }
        }
        private bool SetMultipleSelectionCursor(MultipleSelection.SelectionPart multipleSelectionPart)
        {
            const string help = "Uniform resize of the {0} of all the elements (Shift for centering, Ctrl to keep the ratio).";
            switch (multipleSelectionPart)
            {
                case MultipleSelection.SelectionPart.TopLeft:
                    Cursor.Current = Cursors.PanNW;
                    toolStripStatusLabelHelp.Text = string.Format(help, "top/left");
                    return true;
                case MultipleSelection.SelectionPart.Top:
                    Cursor.Current = Cursors.PanNorth;
                    toolStripStatusLabelHelp.Text = string.Format(help, "top");
                    return true;
                case MultipleSelection.SelectionPart.TopRight:
                    Cursor.Current = Cursors.PanNE;
                    toolStripStatusLabelHelp.Text = string.Format(help, "top/right");
                    return true;
                case MultipleSelection.SelectionPart.Right:
                    Cursor.Current = Cursors.PanEast;
                    toolStripStatusLabelHelp.Text = string.Format(help, "right");
                    return true;
                case MultipleSelection.SelectionPart.BottomRight:
                    Cursor.Current = Cursors.PanSE;
                    toolStripStatusLabelHelp.Text = string.Format(help, "bottom/right");
                    return true;
                case MultipleSelection.SelectionPart.Bottom:
                    Cursor.Current = Cursors.PanSouth;
                    toolStripStatusLabelHelp.Text = string.Format(help, "bottom");
                    return true;
                case MultipleSelection.SelectionPart.BottomLeft:
                    Cursor.Current = Cursors.PanSW;
                    toolStripStatusLabelHelp.Text = string.Format(help, "bottom/left");
                    return true;
                case MultipleSelection.SelectionPart.Left:
                    Cursor.Current = Cursors.PanWest;
                    toolStripStatusLabelHelp.Text = string.Format(help, "left");
                    return true;
                default:
                    Cursor.Current = Cursors.Arrow;
                    toolStripStatusLabelHelp.Text = "";
                    return false;
            }
        }

        private void InvalidateDisplay()
        {
            this.scrollingControlContainer.ScrolledControl.Invalidate();
        }

    }
}
