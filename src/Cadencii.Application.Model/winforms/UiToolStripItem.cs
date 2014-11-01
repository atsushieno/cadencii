using System;
using cadencii.java.awt;

namespace cadencii
{
	public interface UiToolStripItem : UiComponent, IDisposable
	{
		Font Font {
			get;
			set;
		}

		bool Enabled { get; set; }
		bool Visible { get; set; }
		string Name { get; set; }
		object Tag { get; set; }
		string Text { get; set; }
		string ToolTipText { get; set; }
		string ShortcutKeyDisplayString { get; set; }
		Dimension Size { get; set; }

		void PerformClick ();

		event EventHandler MouseEnter;
		event EventHandler Click;
	}
}
