using System;
using cadencii.java.awt;
using System.Collections.Generic;

namespace cadencii
{
	public interface UiContextMenu : UiControl
	{
		List<UiMenuItem> MenuItems { get; }

		void Show (UiControl control, Point point);
	}
}

