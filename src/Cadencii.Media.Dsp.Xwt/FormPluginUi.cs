﻿/*
 * FormPluginUi.cs
 * Copyright © 2009-2011 kbinani
 *
 * This file is part of cadencii.
 *
 * cadencii is free software; you can redistribute it and/or
 * modify it under the terms of the GPLv3 License.
 *
 * cadencii is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 */
using Cadencii.Gui;
using Cadencii.Utilities;
using Cadencii.Platform.Windows;


#if ENABLE_AQUESTONE
using System;
using Xwt;

namespace cadencii
{


	class FormPluginUi : Window
	{
		private System.ComponentModel.IContainer components;
		IntPtr childWnd = IntPtr.Zero;
		private double lastDrawn = 0.0;

		public FormPluginUi()
		{
			//this.SetStyle(System.Windows.Forms.ControlStyles.DoubleBuffer, true);
			//this.SetStyle(System.Windows.Forms.ControlStyles.UserPaint, true);
			InitializeComponent();
			this.CloseRequested += FormPluginUi_FormClosing;
			// FIXME: bring this back
			//this.Icon = cadencii.Properties.Resources._switch;
		}

		public bool IsOpened {
			get;
			set;
		}

		public bool IsDisposed {
			get;
			set;
		}

		public IntPtr Handle {
			get { throw new NotImplementedException (); }
		}

		internal Xwt.Size WindowRect = new Xwt.Size(373, 158);

		void FormPluginUi_FormClosing(Object sender, CloseRequestedEventArgs e)
		{
			e.AllowClose = false;
			this.Visible = false;
		}

		private void InitializeComponent()
		{
			this.Size = new Xwt.Size(334, 164);
			this.Resizable = false;
			this.Title = "FormPluginUi";
		}

		public void invalidateUi()
		{
			double now = PortUtil.getCurrentTime();

			if (now - lastDrawn > 0.04) {
				if (childWnd != IntPtr.Zero) {
					bool ret = false;
					try {
						ret = win32.InvalidateRect(childWnd, IntPtr.Zero, false);
					} catch (Exception ex) {
						Logger.StdErr("FormPluginUi#invalidateUi; ex=" + ex);
						ret = false;
					}
					lastDrawn = now;
				}
			}
		}

		internal void UpdatePluginUiRect()
		{
			try {
				win32.EnumChildWindows(Handle, EnumChildProc, 0);
			} catch (Exception ex) {
				Logger.StdErr("vstidrv#updatePluginUiRect; ex=" + ex);
			}
		}

		private bool EnumChildProc(IntPtr hwnd, int lParam)
		{
			RECT rc = new RECT();
			try {
				win32.GetWindowRect(hwnd, ref rc);
			} catch (Exception ex) {
				Logger.StdErr("vstidrv#enumChildProc; ex=" + ex);
			}
			childWnd = hwnd;
			WindowRect = new Xwt.Size(rc.right - rc.left, rc.bottom - rc.top);
			return false; //最初のやつだけ検出できればおｋなので
		}
	}

}
#endif