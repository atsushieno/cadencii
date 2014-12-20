using System;
using WClipboard = System.Windows.Forms.Clipboard;
using cadencii;

namespace Cadencii.Application.Forms
{
	class ClipboardWF : Clipboard
	{
		public override void SetText (string value)
		{
			WClipboard.SetText (value);
		}

		public override string GetText ()
		{
			return WClipboard.GetText ();
		}

		public override void SetDataObject (object data, bool copy)
		{
			WClipboard.SetDataObject (data, copy);
		}

		public override object GetDataObject (Type dataType)
		{
			var dobj = WClipboard.GetDataObject ();
			if (dobj != null)
				return dobj.GetData (dataType);
			return null;
		}
	}
}

