using System;
using cadencii.java.awt;

namespace cadencii
{
	public interface UiPictureBox : UiControl
	{
		BorderStyle BorderStyle { get; set; }

		PictureBoxSizeMode SizeMode { get; set; }

		Dimension MaximumSize { get; set; }
		Dimension MinimumSize { get; set; }

		Image Image { get; set; }

		event EventHandler<PaintEventArgs> Paint;
	}
}

