using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static KMLEditor.UndoManager;

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

        private KMLElementWithOffset draggingElement = null;
        private KMLElementWithOffset.SelectionPart draggingPart = KMLElementWithOffset.SelectionPart.None;
        private Point draggingStartLocation;
        private List<KMLElementWithOffset> draggingSelectedElements = new List<KMLElementWithOffset>();

        private static SolidBrush brushForeground = new SolidBrush(Color.DarkGoldenrod);
        private static Pen penForeground = new Pen(brushForeground);
        private static Pen penForegroundButtonDown = new Pen(brushForeground);
        private static SolidBrush brushForegroundSelected = new SolidBrush(Color.Red);
        private static SolidBrush brushForegroundButtonSelected = new SolidBrush(Color.Pink);
        private static SolidBrush brushSelectionColor = new SolidBrush(Color.Gray);
        private static Pen penForegroundSelected = new Pen(brushForegroundSelected);
        private static Pen penForegroundButtonSelected = new Pen(brushForegroundButtonSelected);
        private static Pen penForegroundButtonDownSelected = new Pen(brushForegroundSelected);
        private static Pen penSelection = new Pen(brushSelectionColor);
        private Point pointSelectionStartLocation = new Point();
        private Rectangle rectangleScaledSelection = new Rectangle();
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

            penSelection.DashStyle = DashStyle.Dash;
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
            foreach (var kmlFile in kmlFileManager.GetFiles())
                if (kmlFile.isDirty)
                    if (MessageBox.Show("At least one file is not saved. Quit anyway?", "Exit", MessageBoxButtons.YesNo) == DialogResult.No)
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
            List<DoOperation> doOperations = undoManager.Undo();
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
            List<DoOperation> doOperations = undoManager.Redo();
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
            InvalidateDisplay();
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
            e.Graphics.TranslateTransform(-(float)virtualPoint.X, -(float)virtualPoint.Y);
            e.Graphics.ScaleTransform(zoom, zoom);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            if(zoom > 1.0f)
            {
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.None;
            }

            if (bitmap != null)
                graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            KMLElementWithOffset lastSelectedElement = GetLastSelectedElement();
            foreach (var element in kmlFileManager.GetElementsWithOffsetAndSize())
            {
                Pen pen = element.isSelected? penForegroundSelected : penForeground;
                Rectangle rectangle = element.GetBoundForPart(KMLElementWithOffset.SelectionPart.Element);
                if (element == lastSelectedElement)
                {
                    if(element is KMLLcd)
                    {
                        graphics.DrawLine(penForegroundButtonSelected, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Top);
                        graphics.DrawLine(penForegroundButtonSelected, rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom);
                    }
                    else
                    {
                        graphics.DrawRectangle(penForegroundButtonSelected, rectangle);
                        graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.TopLeft));
                        graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.TopRight));
                        graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.BottomRight));
                        graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.BottomLeft));
                        if (element is KMLElementWithOffsetAndSizeAndDown)
                        {
                            KMLElementWithOffsetAndSizeAndDown kmlElementWithOffsetAndSizeAndDown = (KMLElementWithOffsetAndSizeAndDown)element;
                            if (kmlElementWithOffsetAndSizeAndDown.DownX != null && kmlElementWithOffsetAndSizeAndDown.DownY != null)
                                graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.InsideDown));
                        }
                    }
                    graphics.DrawRectangle(penForegroundButtonSelected, element.GetBoundForPart(KMLElementWithOffset.SelectionPart.Inside));
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
                        graphics.DrawRectangle(element.isSelected ? penForegroundButtonDownSelected : penForegroundButtonDown,
                            new Rectangle((int)kmlElementWithOffsetAndSizeAndDown.DownX, (int)kmlElementWithOffsetAndSizeAndDown.DownY, kmlElementWithOffsetAndSizeAndDown.SizeWidth, kmlElementWithOffsetAndSizeAndDown.SizeHeight));
                }
            }

            if(showSelectionRectangle)
                graphics.DrawRectangle(penSelection, rectangleScaledSelection);
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
                        draggingSelectedElements.Clear();
                        foreach (var kmlElement in selectedElements)
                        {
                            kmlElement.BackupForDragging();
                            // Deep copy for undo
                            KMLElementWithOffset kmlElementWithOffsetClone = kmlElement.Clone() as KMLElementWithOffset;
                            if(kmlElementWithOffsetClone != null)
                                draggingSelectedElements.Add(kmlElementWithOffsetClone);
                        }
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
                pointSelectionStartLocation = e.Location;
                selectedElementsBeforeRectangleSelection.Clear();
                selectedElementsBeforeRectangleSelection.AddRange(selectedElements);
            }

            Point location = new Point((int)((e.Location.X + scrollingControlContainer.VirtualPoint.X) / zoom), (int)((e.Location.Y + scrollingControlContainer.VirtualPoint.Y) / zoom));

            toolStripStatusLabelCoordinates.Text = string.Format("{0},{1}", location.X, location.Y);

            int deltaX = e.Location.X - pointSelectionStartLocation.X;
            int deltaY = e.Location.Y - pointSelectionStartLocation.Y;

            if (showSelectionRectangle && deltaX != 0 && deltaY != 0)
            {
                // Update the rectangular selection
                rectangleScaledSelection = new Rectangle(
                    (int)((pointSelectionStartLocation.X + scrollingControlContainer.VirtualPoint.X) / zoom),
                    (int)((pointSelectionStartLocation.Y + scrollingControlContainer.VirtualPoint.Y) / zoom),
                    (int)(deltaX / zoom),
                    (int)(deltaY / zoom)
                );

                // If the rectangle has a negative size, make it positif
                if (rectangleScaledSelection.Width < 0)
                    rectangleScaledSelection.X -= rectangleScaledSelection.Width = -rectangleScaledSelection.Width;
                if (rectangleScaledSelection.Height < 0)
                    rectangleScaledSelection.Y -= rectangleScaledSelection.Height = -rectangleScaledSelection.Height;

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
                    if (rectangleScaledSelection.IntersectsWith(element.GetBoundForPart(KMLElementWithOffset.SelectionPart.Element)))
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

                toolStripStatusLabelCoordinates.Text = string.Format("{0},{1} {2}x{3}", rectangleScaledSelection.X, rectangleScaledSelection.Y, rectangleScaledSelection.Width, rectangleScaledSelection.Height);
            }
            else if (draggingElement != null)
            {
                // Pan or resize the selected elements
                Point draggingDelta = new Point((int)((e.Location.X - draggingStartLocation.X) / zoom), (int)((e.Location.Y - draggingStartLocation.Y) / zoom));
                foreach (var kmlElement in selectedElements)
                    kmlElement.UpdateForDragging(draggingDelta, draggingPart, (ModifierKeys & Keys.Shift) == Keys.Shift, toolStripButtonAllowMoveDownPart.Checked); // (KMLElementWithOffset.MovePart)toolStripComboBoxDownPart.SelectedIndex);
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
                InvalidateDisplay();
            }
            else if (draggingElement != null && (ModifierKeys & Keys.Control) != Keys.Control)
            {
                // End modifying a KML element
                if (draggingSelectedElements.Count > 0)
                {
                    List<DoOperation> operations = new List<DoOperation>();
                    foreach (var kmlElementWithOffset in draggingSelectedElements)
                    {
                        operations.Add(new DoOperation
                        {
                            Before = kmlElementWithOffset,
                            After = kmlFileManager.GetElementWithOffsetById(kmlElementWithOffset.id).Clone(),
                            OperationType = OperationType.PanOrResize,
                            ModifiedPart = draggingPart
                        });
                    }
                    undoManager.Modify(operations);
                    UpdatePropertyGridAndListBoxForSelectedElement();
                    UpdateUndoMenuAndToolbar();
                    UpdateKMLSource();
                }
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
                UpdatePropertyGridAndListBoxForSelectedElement();
                InvalidateDisplay();
            }

            draggingElement = null;
            draggingPart = KMLElementWithOffset.SelectionPart.None;

            leftMouseButtonDownPulse = false;
            showSelectionRectangle = false;
            return true;
        }

        private void listBoxSelectedElement_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (preventListBoxSelectedElementEvent)
                return;
            foreach (var kmlElementWithOffset in kmlFileManager.GetElementsWithOffsetAndSize())
                kmlElementWithOffset.isSelected = listBoxSelectedElement.SelectedItems.Contains(kmlElementWithOffset);
            UpdateSelectedElementsList();
            UpdatePropertyGridForSelectedElement();
            InvalidateDisplay();
        }

        private void propertyGridSelectedElement_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            string property = e.ChangedItem.Label;
            int? oldValue = (int)e.OldValue;
            List<DoOperation> operations = new List<DoOperation>();
            foreach (var kmlElement in propertyGridSelectedElement.SelectedObjects)
            {
                if(kmlElement is KMLElementWithOffset)
                {
                    KMLElementWithOffset kmlElementWithOffset = (KMLElementWithOffset)kmlElement;
                    DoOperation operation = new DoOperation
                    {
                        Before = kmlElementWithOffset.Clone(),
                        After = kmlElementWithOffset.Clone(),
                        OperationType = OperationType.PanOrResize,
                        ModifiedPart = draggingPart
                    };
                    switch (property)
                    {
                        case "OffsetX":
                            if(oldValue != null && kmlElement is KMLElementWithOffset)
                            {
                                ((KMLElementWithOffset)operation.Before).OffsetX = (int)oldValue;
                                operation.OperationType = OperationType.PanOrResize;
                                operation.ModifiedPart = KMLElementWithOffset.SelectionPart.Left;
                            }
                            break;
                        case "OffsetY":
                            if (oldValue != null && kmlElement is KMLElementWithOffset)
                            {
                                ((KMLElementWithOffset)operation.Before).OffsetY = (int)oldValue;
                                operation.OperationType = OperationType.PanOrResize;
                                operation.ModifiedPart = KMLElementWithOffset.SelectionPart.Top;
                            }
                            break;
                        case "SizeWidth":
                            if (oldValue != null && kmlElement is KMLElementWithOffsetAndSize)
                            {
                                ((KMLElementWithOffsetAndSize)operation.Before).SizeWidth = (int)oldValue;
                                operation.OperationType = OperationType.PanOrResize;
                                operation.ModifiedPart = KMLElementWithOffset.SelectionPart.Right;
                            }
                            break;
                        case "SizeHeight":
                            if (oldValue != null && kmlElement is KMLElementWithOffsetAndSize)
                            {
                                ((KMLElementWithOffsetAndSize)operation.Before).SizeHeight = (int)oldValue;
                                operation.OperationType = OperationType.PanOrResize;
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
            UpdatePropertyGridAndListBoxForSelectedElement();
        }

        private void UpdateAfterDoOperation()
        {
            UpdateSelectedElementsList();
            UpdatePropertyGridAndListBoxForSelectedElement();
            UpdateUndoMenuAndToolbar();
            InvalidateDisplay();
            UpdateKMLSource();
        }

        private void UpdateSelectedElementsList()
        {
            selectedElements.Clear();
            selectedElements.AddRange(kmlFileManager.GetElementsWithOffsetAndSize().Where(element => element.isSelected));
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

        private static void SetSelectionCursor(KMLElementWithOffset.SelectionPart selectionPart)
        {
            switch (selectionPart)
            {
                case KMLElementWithOffset.SelectionPart.Inside:
                case KMLElementWithOffset.SelectionPart.InsideDown:
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
