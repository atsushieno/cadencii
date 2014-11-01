
using System;
using System.Collections.Generic;
using cadencii.java.awt;

namespace cadencii
{
	public interface UiImageList : UiComponent
	{
		ColorDepth ColorDepth {
			get;
			set;
		}

		object ImageStream {
			get;
			set;
		}

		Dimension ImageSize {
			get;
			set;
		}

		void SetImagesKeyName (int i, string diskpluspng);

		Color TransparentColor {
			get;
			set;
		}

		List<Image> Images { get; }
	}

}

