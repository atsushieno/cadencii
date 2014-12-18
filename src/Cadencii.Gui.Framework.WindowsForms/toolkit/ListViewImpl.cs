using System;
using System.Linq;
using Cadencii.Gui;
using Keys = Cadencii.Gui.Toolkit.Keys;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;
using MouseButtons = System.Windows.Forms.MouseButtons;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using MouseEventHandler = System.Windows.Forms.MouseEventHandler;
using NKeyEventArgs = Cadencii.Gui.Toolkit.KeyEventArgs;
using NKeyPressEventArgs = Cadencii.Gui.Toolkit.KeyPressEventArgs;
using NKeyEventHandler = System.EventHandler<Cadencii.Gui.Toolkit.KeyEventArgs>;
using NMouseButtons = Cadencii.Gui.Toolkit.MouseButtons;
using NMouseEventArgs = Cadencii.Gui.Toolkit.MouseEventArgs;
using NMouseEventHandler = System.EventHandler<Cadencii.Gui.Toolkit.MouseEventArgs>;
using System.Collections.Generic;
using Cadencii.Gui.Toolkit;
using cadencii;
using System.Collections.ObjectModel;
using System.Collections;

namespace Cadencii.Gui.Toolkit
{
	public class ListViewImpl : System.Windows.Forms.ListView, UiListView
	{
		// UiListView

		void UiListView.AddItem (UiListViewItem item)
		{
			Items.Add ((System.Windows.Forms.ListViewItem) item.Native);
		}

		void UiListView.RemoveItemAt (int i)
		{
			Items.RemoveAt (i);
		}

		IList UiListView.SelectedIndices {
			get { return SelectedIndices; }
		}

		int UiListView.ItemCount {
			get { return Items.Count; }
		}

		void UiListView.AddRow(string[] items, bool selected)
		{
			WinformsExtensions.AddRow (this, items, selected);
		}

		UiListViewItem UiListView.GetItem (int i)
		{
			return new ListViewItemImpl (Items [i]);
		}
		void UiListView.ClearItems ()
		{
			Items.Clear ();
		}

		IList<UiListViewColumn> UiListView.Columns {
			get { return columns ?? (columns = new ColumnCollection (this)); }
		}

		Collection<UiListViewColumn> columns;

		class ColumnCollection : Collection<UiListViewColumn>
		{
			System.Windows.Forms.ListView lv;
			public ColumnCollection (System.Windows.Forms.ListView lv)
			{
				this.lv = lv;
			}

			protected override void InsertItem (int index, UiListViewColumn item)
			{
				base.InsertItem (index, item);
				lv.Columns.Add ((System.Windows.Forms.ColumnHeader) item.Native);
			}

			protected override void ClearItems ()
			{
				base.ClearItems ();
				lv.Columns.Clear ();
			}

			protected override void RemoveItem (int index)
			{
				base.RemoveItem (index);
				lv.Columns.RemoveAt (index);
			}

			protected override void SetItem (int index, UiListViewColumn item)
			{
				throw new NotSupportedException ();
			}
		}

		/*
		void UiListView.AddGroups (IEnumerable<UiListViewGroup> groups)
		{
			Groups.AddRange (groups.Select (g => (System.Windows.Forms.ListViewGroup) g.Native).ToArray ());
		}
		*/
		Cadencii.Gui.Toolkit.View UiListView.View {
			get { return (Cadencii.Gui.Toolkit.View) View; }
			set { View = (System.Windows.Forms.View) value; } 
		}

		// UiControl

		UiControl UiControl.Parent {
			get { return Parent as UiControl; }
		}

		Cursor UiControl.Cursor {
			get { return this.Cursor.ToAwt (); }
			set { Cursor = value.ToNative (); }
		}

		IList<UiControl> UiControl.Controls {
			get { return new CastingList<UiControl, System.Windows.Forms.Control> (Items, null, null); }
		}

		event EventHandler UiControl.SizeChanged {
			add { this.SizeChanged += value; }
			remove { this.SizeChanged -= value; }
		}

		object UiControl.Native {
			get { return this; }
		}

		bool UiControl.IsDisposed {
			get { return IsDisposed; }
		}

		AnchorStyles UiControl.Anchor {
			get { return (AnchorStyles)Anchor; }
			set { Anchor = (System.Windows.Forms.AnchorStyles) value; }
		}

		Rectangle UiControl.Bounds {
			get { return Bounds.ToAwt (); }
			set { this.Bounds = value.ToWF (); }
		}

		ImeMode UiControl.ImeMode {
			get { return (ImeMode)ImeMode; }
			set { ImeMode = (System.Windows.Forms.ImeMode) value; }
		}

		Font UiControl.Font {
			get { return Font.ToAwt (); }
			set { Font = value.ToWF (); }
		}

		Color UiControl.ForeColor {
			get { return ForeColor.ToAwt (); }
			set { ForeColor = value.ToNative (); }
		}

		Color UiControl.BackColor {
			get { return BackColor.ToAwt (); }
			set { BackColor = value.ToNative (); }
		}

		Point UiControl.Location {
			get { return Location.ToAwt (); }
			set { Location = value.ToWF (); }
		}

		Dimension UiControl.Size {
			get { return new Dimension (Size.Width, Size.Height); }
			set { this.Size = new System.Drawing.Size (value.Width, value.Height); }
		}

		Padding UiControl.Margin {
			get { return new Padding (Margin.All); }
			set { Margin = new System.Windows.Forms.Padding (value.All); }
		}

		DockStyle UiControl.Dock {
			get { return (DockStyle)Dock; }
			set { Dock = (System.Windows.Forms.DockStyle)value; }
		}

		void UiControl.Focus ()
		{
			Focus ();
		}

		void UiControl.Invalidate ()
		{
			Invalidate ();
		}

		Point UiControl.PointToClient (Point point)
		{
			return PointToClient (point.ToWF ()).ToAwt ();
		}

		Point UiControl.PointToScreen (Point point)
		{
			return PointToScreen (point.ToWF ()).ToAwt ();
		}

		event EventHandler UiControl.Enter {
			add { Enter += value; }
			remove { Enter -= value; }
		}

		event EventHandler UiControl.Resize {
			add { this.Resize += value; }
			remove { this.Resize -= value; }
		}

		event EventHandler UiControl.ImeModeChanged {
			add { this.ImeModeChanged += value; }
			remove { this.ImeModeChanged -= value; }
		}

		event NKeyEventHandler UiControl.PreviewKeyDown {
			add { this.PreviewKeyDown += (sender, e) => value (sender, new NKeyEventArgs ((Keys) e.KeyData)); }
			remove { this.PreviewKeyDown -= (sender, e) => value (sender, new NKeyEventArgs ((Keys) e.KeyData)); }
		}

		event EventHandler<KeyPressEventArgs> UiControl.KeyPress {
			add { this.KeyPress += (sender,  e) => value (sender, new NKeyPressEventArgs (e.KeyChar) { Handled = e.Handled}); }
			remove { this.KeyPress -= (sender,  e) => value (sender, new NKeyPressEventArgs (e.KeyChar) { Handled = e.Handled}); }
		}

		event NKeyEventHandler UiControl.KeyUp {
			add { this.KeyUp += (sender, e) => value (sender, new NKeyEventArgs ((Keys) e.KeyData)); }
			remove { this.KeyUp -= (sender, e) => value (sender, new NKeyEventArgs ((Keys) e.KeyData)); }
		}

		event NKeyEventHandler UiControl.KeyDown {
			add { this.KeyDown += (sender, e) => value (sender, new NKeyEventArgs ((Keys) e.KeyData)); }
			remove { this.KeyDown -= (sender, e) => value (sender, new NKeyEventArgs ((Keys) e.KeyData)); }
		}

		event NMouseEventHandler UiControl.MouseClick {
			add { this.MouseClick += (sender, e) => value (sender, e.ToAwt ()); }
			remove { this.MouseClick -= (sender, e) => value (sender, e.ToAwt ()); }
		}

		event NMouseEventHandler UiControl.MouseDoubleClick {
			add { this.MouseDoubleClick += (sender, e) => value (sender, e.ToAwt ()); }
			remove { this.MouseDoubleClick -= (sender, e) => value (sender, e.ToAwt ()); }
		}

		event NMouseEventHandler UiControl.MouseDown {
			add { this.MouseDown += (sender, e) => value (sender, e.ToAwt ()); }
			remove { this.MouseDown -= (sender, e) => value (sender, e.ToAwt ()); }
		}

		event NMouseEventHandler UiControl.MouseUp {
			add { this.MouseUp += (sender, e) => value (sender, e.ToAwt ()); }
			remove { this.MouseUp -= (sender, e) => value (sender, e.ToAwt ()); }
		}

		event NMouseEventHandler UiControl.MouseMove {
			add { this.MouseMove += (sender, e) => value (sender, e.ToAwt ()); }
			remove { this.MouseMove -= (sender, e) => value (sender, e.ToAwt ()); }
		}
		event NMouseEventHandler UiControl.MouseWheel {
			add { this.MouseWheel += (sender, e) => value (sender, e.ToAwt ()); }
			remove { this.MouseWheel -= (sender, e) => value (sender, e.ToAwt ()); }
		}
	}
}

