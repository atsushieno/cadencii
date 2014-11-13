using System;
using System.Collections;
using Cadencii.Gui;

namespace Cadencii.Application.Controls
{
	public interface RebarBandCollection : IList
	{
		//int NextID ();
		Rebar Rebar { get; }
		RebarBand this [int i] { get; }
		RebarBand this [string name] { get; }
	}
}

