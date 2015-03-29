/*
 * FormNotePropertyUiImpl.cs
 * Copyright © 2012 kbinani
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
using System;
using cadencii.apputil;
using Cadencii.Gui;
using Cadencii.Gui.Toolkit;

namespace Cadencii.Application.Forms
{
    public class FormNotePropertyImpl : FormImpl, UiFormNoteProperty
    {
        private FormNotePropertyListener listener;
        protected FormWindowState lastWindowState = FormWindowState.Normal;

        public FormNotePropertyImpl(FormNotePropertyListener listener)
        {
            this.listener = listener;
            InitializeComponent();
            GuiHost.Current.ApplyFontRecurse(this, EditorManager.editorConfig.getBaseFont());
        }


        #region FormNotePropertyUiの実装

        public void addComponent(object c)
        {
            if (c == null) {
                return;
            }
            if (!(c is UiControl)) {
                return;
            }
            var control = (UiControl)c;
			this.AsGui ().AddControl(control);
            control.Dock = DockStyle.Fill;
        }

        public bool isWindowMinimized()
        {
			return this.AsGui ().WindowState == FormWindowState.Minimized;
        }

        public void deiconfyWindow()
        {
			this.AsGui ().WindowState = FormWindowState.Normal;
        }

        public void setTitle(string title)
        {
			this.AsGui ().Text = title;
        }

        public void close()
        {
			this.AsGui ().Close ();
        }

        public void setMenuCloseAccelerator(Keys value)
        {
            this.menuClose.ShortcutKeys = value;
        }

        public void setAlwaysOnTop(bool alwaysOnTop)
        {
			this.AsGui ().TopMost = alwaysOnTop;
        }

        public bool isAlwaysOnTop()
        {
			return this.AsGui ().TopMost;
        }

        public void setBounds(int x, int y, int width, int height)
        {
			this.AsGui ().Bounds = new Rectangle(x, y, width, height);
        }

        public int getX()
        {
			return this.AsGui ().Location.X;
        }

        public int getY()
        {
			return this.AsGui ().Location.Y;
        }

        public int getWidth()
        {
			return this.AsGui ().Width;
        }

        public int getHeight()
        {
			return this.AsGui ().Height;
        }

        public void setVisible(bool visible)
        {
            this.Visible = visible;
        }

        public bool isVisible()
        {
            return this.Visible;
        }

        public int getWorkingAreaX()
        {
            Rectangle r = Screen.Instance.GetWorkingArea(this);
            return r.X;
        }

        public int getWorkingAreaY()
        {
            Rectangle r = Screen.Instance.GetWorkingArea(this);
            return r.Y;
        }

        public int getWorkingAreaWidth()
        {
            Rectangle r = Screen.Instance.GetWorkingArea(this);
            return r.Width;
        }

        public int getWorkingAreaHeight()
        {
            Rectangle r = Screen.Instance.GetWorkingArea(this);
            return r.Height;
        }

        public void hideWindow()
        {
            this.Visible = false;
        }

        #endregion

		// FIXME: I don't think xwt can support this.
		/*
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (this.lastWindowState != this.WindowState) {
                this.listener.windowStateChanged();
            }
            this.lastWindowState = this.WindowState;
        }
        */

        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
			ApplicationUIHost.Instance.ApplyXml (this, "FormNoteProperty.xml");

			this.ParentWindow.Shown += FormNotePropertyUiImpl_Load;
			this.ParentWindow.BoundsChanged += FormNotePropertyUiImpl_SizeChanged;
			this.ParentWindow.CloseRequested += FormNotePropertyUiImpl_FormClosing;
			menuClose.Click += new System.EventHandler (menuClose_Click);
        }

        #endregion

		#pragma warning disable 0169,0649
        UiMenuStrip menuStrip;
        UiToolStripMenuItem menuWindow;
        UiToolStripMenuItem menuClose;
		#pragma warning restore 0169,0649

        private void FormNotePropertyUiImpl_Load(object sender, System.EventArgs e)
        {
            this.listener.onLoad();
        }

        private void menuClose_Click(object sender, System.EventArgs e)
        {
            this.listener.menuCloseClick();
        }

        private void FormNotePropertyUiImpl_SizeChanged(object sender, System.EventArgs e)
        {
            this.listener.locationOrSizeChanged();
        }

        private void FormNotePropertyUiImpl_FormClosing(object sender, Xwt.CloseRequestedEventArgs e)
        {
			// FIXME: dunno if we can support that. Just close it.
			/*
            if (e.CloseReason != System.Windows.Forms.CloseReason.UserClosing) {
                return;
            }
            e.Cancel = true;
            this.listener.formClosing();
            */
        }
    }
}