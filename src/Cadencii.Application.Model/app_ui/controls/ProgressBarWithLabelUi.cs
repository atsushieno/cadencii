using System;
using Cadencii.Gui.Toolkit;

namespace Cadencii.Application.Controls
{
	public interface ProgressBarWithLabelUi : UiControl
	{
		int Progress { get; set; }
		string Text { get; set; }
	}
}

