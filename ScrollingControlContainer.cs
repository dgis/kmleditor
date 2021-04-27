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
using System.Windows.Forms;

namespace KMLEditor
{
    class ScrollingControlContainer : UserControl
	{
		private ScrolledControl scrolledControl;
		private VScrollBar vScrollBar;
		private HScrollBar hScrollBar;
		private Point virtualPoint = new Point(0, 0);
		private Size virtualSize = new Size(10, 10);
		private bool isDragging = false;
		private Point mouseDownPosition = new Point(0, 0);
		private int mouseDownHScroll = 0;
		private int mouseDownVScroll = 0;

		public ScrolledControl ScrolledControl { get { return this.scrolledControl; } }
        public delegate bool ContentMouseEventHandler(object sender, MouseEventArgs e);
        public event ContentMouseEventHandler ContentMouseDown;
        public event ContentMouseEventHandler ContentMouseMove;
		public event ContentMouseEventHandler ContentMouseUp;
		public event ContentMouseEventHandler ContentMouseWheel;


		public Point VirtualPoint
		{
			get { return this.virtualPoint; }
			set { this.virtualPoint = value; }
		}

		public Size VirtualSize
		{
			get { return this.virtualSize; }
			set { this.virtualSize = value; scrolledControl_Resize(null, null); }
		}

		public ScrollingControlContainer()
        {
            InitializeComponent();

			this.scrolledControl.MouseWheel += new MouseEventHandler(scrolledControl_MouseWheel);
		}

		private void InitializeComponent()
		{
			this.vScrollBar = new VScrollBar();
			this.hScrollBar = new HScrollBar();
			this.scrolledControl = new ScrolledControl();
			this.SuspendLayout();
			// 
			// vScrollBar
			// 
			this.vScrollBar.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Bottom)
						| AnchorStyles.Right)));
			this.vScrollBar.Location = new Point(437, 0);
			this.vScrollBar.Name = "vScrollBar";
			this.vScrollBar.Size = new Size(17, 348);
			this.vScrollBar.SmallChange = 5;
			this.vScrollBar.TabIndex = 0;
			this.vScrollBar.ValueChanged += new EventHandler(this.vScrollBar_ValueChanged);
			// 
			// hScrollBar
			// 
			this.hScrollBar.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left)
						| AnchorStyles.Right)));
			this.hScrollBar.Location = new Point(0, 348);
			this.hScrollBar.Name = "hScrollBar";
			this.hScrollBar.Size = new Size(437, 17);
			this.hScrollBar.SmallChange = 5;
			this.hScrollBar.TabIndex = 1;
			this.hScrollBar.ValueChanged += new EventHandler(this.vScrollBar_ValueChanged);
            // 
            // scrolledControl
            // 
            this.scrolledControl.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
						| AnchorStyles.Left)
						| AnchorStyles.Right)));
			this.scrolledControl.Location = new Point(0, 0);
			this.scrolledControl.Margin = new Padding(0);
			this.scrolledControl.Name = "scrolledControl";
			this.scrolledControl.Size = new Size(437, 348);
			this.scrolledControl.TabIndex = 2;
			this.scrolledControl.MouseDown += new MouseEventHandler(this.scrolledControl_MouseDown);
			this.scrolledControl.MouseMove += new MouseEventHandler(this.scrolledControl_MouseMove);
			this.scrolledControl.MouseUp += new MouseEventHandler(this.scrolledControl_MouseUp);
            this.scrolledControl.Resize += new EventHandler(this.scrolledControl_Resize);
            // 
            // ScrollingControlContainer
            // 
            this.Controls.Add(this.scrolledControl);
			this.Controls.Add(this.hScrollBar);
			this.Controls.Add(this.vScrollBar);
			this.Name = "ScrollingControlContainer";
			this.Size = new Size(454, 365);
			this.ResumeLayout(false);

		}

		void scrolledControl_MouseWheel(object sender, MouseEventArgs e)
		{
			if (ContentMouseWheel != null && ContentMouseWheel.Invoke(this, e))
				return;

			if ((ModifierKeys & Keys.Control) != Keys.Control)
			{
				int scrollBarValue = this.vScrollBar.Value;
				scrollBarValue -= e.Delta;
				if (scrollBarValue > this.vScrollBar.Maximum - this.vScrollBar.LargeChange)
					scrollBarValue = this.vScrollBar.Maximum - this.vScrollBar.LargeChange;
				if (scrollBarValue < this.vScrollBar.Minimum)
					scrollBarValue = this.vScrollBar.Minimum;
				this.vScrollBar.Value = scrollBarValue;
			}
		}

		private void scrolledControl_Resize(object sender, EventArgs e)
		{
			LayoutScrollBars();
		}

		public void Clear()
		{
			this.hScrollBar.Value = 0;
			this.vScrollBar.Value = 0;
			LayoutScrollBars();
		}

		public void LayoutScrollBars()
		{
			this.SuspendLayout();
			Size scrolledControlNewSize = this.ClientSize;
			if (this.virtualSize.Width > this.scrolledControl.ClientSize.Width)
			{
				this.hScrollBar.Minimum = 0;
				this.hScrollBar.Maximum = this.virtualSize.Width;
				this.hScrollBar.LargeChange = this.scrolledControl.ClientSize.Width;
				scrolledControlNewSize.Height -= this.hScrollBar.Height;
			}
			else
			{
				this.hScrollBar.Minimum = 0;
				this.hScrollBar.Maximum = 0;
			}

			if (this.virtualSize.Height > this.scrolledControl.ClientSize.Height)
			{
				this.vScrollBar.Minimum = 0;
				this.vScrollBar.Maximum = this.virtualSize.Height;
				this.vScrollBar.LargeChange = this.scrolledControl.ClientSize.Height;
				scrolledControlNewSize.Width -= this.vScrollBar.Width;
			}
			else
			{
				this.vScrollBar.Minimum = 0;
				this.vScrollBar.Maximum = 0;
			}

            // Fix the Bottom right corner of the scrollbar area.
            // Seems to be an issue with Mono on Linux!!!
            //if (!Options.IsRunningOnMono)
            {
                Size hScrollBarSize = this.hScrollBar.Size;
                if (this.hScrollBar.Maximum > 0 && this.vScrollBar.Maximum == 0)
                    hScrollBarSize.Width = this.ClientSize.Width;
                else
                    hScrollBarSize.Width = this.ClientSize.Width - this.vScrollBar.Width;
                this.hScrollBar.Size = hScrollBarSize;

                Size vScrollBarSize = this.vScrollBar.Size;
                if (this.vScrollBar.Maximum > 0 && this.hScrollBar.Maximum == 0)
                    vScrollBarSize.Height = this.ClientSize.Height;
                else
                    vScrollBarSize.Height = this.ClientSize.Height - this.hScrollBar.Height;
                this.vScrollBar.Size = vScrollBarSize;
            }

            this.scrolledControl.Size = scrolledControlNewSize;
			this.scrolledControl.Invalidate(this.scrolledControl.ClientRectangle);
			this.ResumeLayout();
		}

		public void ScrollTo(Point point)
		{
			ScrollTo(point, false);
		}

		public void ScrollTo(Point point, bool center)
		{
			if (center)
			{
				point.X -= this.ScrolledControl.ClientRectangle.Width / 2;
				point.Y -= this.ScrolledControl.ClientRectangle.Height / 2;
			}

			if (point.X > this.hScrollBar.Maximum - this.hScrollBar.LargeChange)
				point.X = this.hScrollBar.Maximum - this.hScrollBar.LargeChange;
			if (point.X < this.hScrollBar.Minimum)
				point.X = this.hScrollBar.Minimum;
			if (point.Y > this.vScrollBar.Maximum - this.vScrollBar.LargeChange)
				point.Y = this.vScrollBar.Maximum - this.vScrollBar.LargeChange;
			if (point.Y < this.vScrollBar.Minimum)
				point.Y = this.vScrollBar.Minimum;

			this.hScrollBar.Value = point.X;
			this.vScrollBar.Value = point.Y;
		}

		private void vScrollBar_ValueChanged(object sender, EventArgs e)
		{
			this.virtualPoint = new Point(this.hScrollBar.Value, this.vScrollBar.Value);
			//System.Diagnostics.Trace.WriteLine("h: " + this.hScrollBar.Value + " (min: " + this.hScrollBar.Minimum + ", max: " + this.hScrollBar.Maximum + "), v: " + this.vScrollBar.Value + " (min: " + this.vScrollBar.Minimum + ", max: " + this.vScrollBar.Maximum + ")");
			this.scrolledControl.Invalidate(this.scrolledControl.ClientRectangle);
		}

        private void scrolledControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (ContentMouseDown != null && ContentMouseDown.Invoke(this, e))
                return;

            if (!this.isDragging)
            {
                this.mouseDownPosition = Cursor.Position;
                this.mouseDownHScroll = this.hScrollBar.Value;
                this.mouseDownVScroll = this.vScrollBar.Value;
                this.isDragging = true;
            }
        }

        private void scrolledControl_MouseMove(object sender, MouseEventArgs e)
		{
            if (ContentMouseMove != null && ContentMouseMove.Invoke(this, e))
                return;

            if (this.isDragging)
			{
				int hValue = this.mouseDownHScroll + (this.mouseDownPosition.X - Cursor.Position.X);
				if (hValue > this.hScrollBar.Minimum && hValue < this.hScrollBar.Maximum - this.hScrollBar.LargeChange)
					this.hScrollBar.Value = hValue;

				int vValue = this.mouseDownVScroll + (this.mouseDownPosition.Y - Cursor.Position.Y);
				if (vValue > this.vScrollBar.Minimum && vValue < this.vScrollBar.Maximum - this.vScrollBar.LargeChange)
					this.vScrollBar.Value = vValue;
			}
		}

		private void scrolledControl_MouseUp(object sender, MouseEventArgs e)
		{
            if (ContentMouseUp != null && ContentMouseUp.Invoke(this, e))
                return;

            this.isDragging = false;
		}
	}
}
