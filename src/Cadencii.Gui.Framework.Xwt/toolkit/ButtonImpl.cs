using System;
using Cadencii.Gui;
using System.Linq;

namespace Cadencii.Gui.Toolkit
{
	public partial class ButtonImpl
	{
		// UiButton

		Cadencii.Gui.Image UiButton.Image {
			get { return this.Image.ToGui (); }
			set { this.Image = value.ToWF (); }
		}

		string IHasText.Text {
			get { return this.Label; }
			set { this.Label = value; }
		}
	}
}
