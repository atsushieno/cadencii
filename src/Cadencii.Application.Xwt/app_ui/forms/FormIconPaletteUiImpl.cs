/*
 * FormIconPalette.cs
 * Copyright © 2010-2011 kbinani
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
using System.Linq;
using System.Collections.Generic;
using cadencii.apputil;
using Cadencii.Gui;
using Cadencii.Media.Vsq;
using Cadencii.Gui.Toolkit;
using Cadencii.Application.Controls;
using cadencii;

namespace Cadencii.Application.Forms
{
    public class FormIconPaletteUiImpl : FormImpl, FormIconPaletteUi
    {
		private List<DraggableBButton> dynaffButtons = new List<DraggableBButton>();
		private List<DraggableBButton> crescendButtons = new List<DraggableBButton>();
		private List<DraggableBButton> decrescendButtons = new List<DraggableBButton>();
        private int buttonWidth = 40;
        private FormMainImpl mMainWindow = null;
        private bool mPreviousAlwaysOnTop;

        public FormIconPaletteUiImpl(FormMainImpl main_window)
        {
            InitializeComponent();
            mMainWindow = main_window;
            applyLanguage();
            GuiHost.Current.ApplyFontRecurse(this, EditorManager.editorConfig.getBaseFont());
            init();
            registerEventHandlers();
            SortedDictionary<string, Keys[]> dict = EditorManager.editorConfig.getShortcutKeysDictionary(mMainWindow.getDefaultShortcutKeys());
            if (dict.ContainsKey("menuVisualIconPalette")) {
                Keys[] keys = dict["menuVisualIconPalette"];
                Keys shortcut = Keys.None;
                keys.Aggregate(shortcut, (seed, key) => seed | key);
                menuWindowHide.ShortcutKeys = shortcut;
            }
        }

        #region public methods
        /// <summary>
        /// AlwaysOnTopが強制的にfalseにされる直前の，AlwaysOnTop値を取得します．
        /// </summary>
        public bool getPreviousAlwaysOnTop()
        {
            return mPreviousAlwaysOnTop;
        }

        /// <summary>
        /// AlwaysOnTopが強制的にfalseにされる直前の，AlwaysOnTop値を設定しておきます．
        /// </summary>
        public void setPreviousAlwaysOnTop(bool value)
        {
            mPreviousAlwaysOnTop = value;
        }

        public void applyLanguage()
        {
            this.Text = _("Icon Palette");
        }

        public void applyShortcut(Keys shortcut)
        {
            menuWindowHide.ShortcutKeys = shortcut;
        }
        #endregion

        #region helper methods
        private static string _(string id)
        {
            return Messaging.getMessage(id);
        }

        private void registerEventHandlers()
        {
            this.Load += new EventHandler(FormIconPalette_Load);
			this.AsGui ().FormClosing += FormIconPalette_FormClosing;
            menuWindowHide.Click += new EventHandler(menuWindowHide_Click);
        }

        private void init()
        {
            foreach (var handle in VocaloSysUtil.dynamicsConfigIterator(SynthesizerType.VOCALOID1)) {
                string icon_id = handle.IconID;
                DraggableBButton btn = ApplicationUIHost.Create<DraggableBButton> ();
                btn.Name = icon_id;
                btn.IconHandle = handle;
                string buttonIconPath = handle.getButtonImageFullPath();

                bool setimg = System.IO.File.Exists(buttonIconPath);
                if (setimg) {
                    btn.Image = new Cadencii.Gui.Image () { NativeImage = Xwt.Drawing.Image.FromStream(new System.IO.FileStream(buttonIconPath, System.IO.FileMode.Open, System.IO.FileAccess.Read)) };
                } else {
                    Xwt.Drawing.Image img = null;
                    string str = "";
                    string caption = handle.IDS;
                    if (caption.Equals("cresc_1")) {
                        img = cadencii.Properties.Resources.cresc1;
                    } else if (caption.Equals("cresc_2")) {
                        img = cadencii.Properties.Resources.cresc2;
                    } else if (caption.Equals("cresc_3")) {
                        img = cadencii.Properties.Resources.cresc3;
                    } else if (caption.Equals("cresc_4")) {
                        img = cadencii.Properties.Resources.cresc4;
                    } else if (caption.Equals("cresc_5")) {
                        img = cadencii.Properties.Resources.cresc5;
                    } else if (caption.Equals("dim_1")) {
                        img = cadencii.Properties.Resources.dim1;
                    } else if (caption.Equals("dim_2")) {
                        img = cadencii.Properties.Resources.dim2;
                    } else if (caption.Equals("dim_3")) {
                        img = cadencii.Properties.Resources.dim3;
                    } else if (caption.Equals("dim_4")) {
                        img = cadencii.Properties.Resources.dim4;
                    } else if (caption.Equals("dim_5")) {
                        img = cadencii.Properties.Resources.dim5;
                    } else if (caption.Equals("Dynaff11")) {
                        str = "fff";
                    } else if (caption.Equals("Dynaff12")) {
                        str = "ff";
                    } else if (caption.Equals("Dynaff13")) {
                        str = "f";
                    } else if (caption.Equals("Dynaff21")) {
                        str = "mf";
                    } else if (caption.Equals("Dynaff22")) {
                        str = "mp";
                    } else if (caption.Equals("Dynaff31")) {
                        str = "p";
                    } else if (caption.Equals("Dynaff32")) {
                        str = "pp";
                    } else if (caption.Equals("Dynaff33")) {
                        str = "ppp";
                    }
                    if (img != null) {
                        btn.Image = new Cadencii.Gui.Image () { NativeImage = img };
                    } else {
                        btn.Text = str;
                    }
                }
                btn.MouseDown += new EventHandler<Cadencii.Gui.Toolkit.MouseEventArgs>(handleCommonMouseDown);
                btn.Size = new Cadencii.Gui.Size(buttonWidth, buttonWidth);
                int iw = 0;
                int ih = 0;
                if (icon_id.StartsWith(IconDynamicsHandle.ICONID_HEAD_DYNAFF)) {
                    // dynaff
                    dynaffButtons.Add(btn);
                    ih = 0;
                    iw = dynaffButtons.Count - 1;
                } else if (icon_id.StartsWith(IconDynamicsHandle.ICONID_HEAD_CRESCEND)) {
                    // crescend
                    crescendButtons.Add(btn);
                    ih = 1;
                    iw = crescendButtons.Count - 1;
                } else if (icon_id.StartsWith(IconDynamicsHandle.ICONID_HEAD_DECRESCEND)) {
                    // decrescend
                    decrescendButtons.Add(btn);
                    ih = 2;
                    iw = decrescendButtons.Count - 1;
                } else {
                    continue;
                }
                btn.Location = new Cadencii.Gui.Point(iw * buttonWidth, ih * buttonWidth);
				this.AsGui ().AddControl(btn);
                btn.BringToFront();
            }

            // ウィンドウのサイズを固定化する
            int height = 0;
            int width = 0;
            if (dynaffButtons.Count > 0) {
                height += buttonWidth;
            }
            width = Math.Max(width, buttonWidth * dynaffButtons.Count);
            if (crescendButtons.Count > 0) {
                height += buttonWidth;
            }
            width = Math.Max(width, buttonWidth * crescendButtons.Count);
            if (decrescendButtons.Count > 0) {
                height += buttonWidth;
            }
            width = Math.Max(width, buttonWidth * decrescendButtons.Count);
            this.ClientSize = new Size(width, height);
            var size = this.Size;
			//this.MaximumSize = size.ToGui ();
			this.MinimumSize = size.ToGui ();
        }
        #endregion

        #region event handlers
        public void FormIconPalette_Load(Object sender, EventArgs e)
        {
            // コンストラクタから呼ぶと、スレッドが違うので（たぶん）うまく行かない
            this.TopMost = true;
        }

		public void FormIconPalette_FormClosing(Object sender, Cadencii.Gui.Toolkit.FormClosingEventArgs e)
        {
            this.Visible = false;
            e.Cancel = true;
        }

        public void menuWindowHide_Click(Object sender, EventArgs e)
        {
            this.Visible = false;
        }

        public void handleCommonMouseDown(Object sender, Cadencii.Gui.Toolkit.MouseEventArgs e)
        {
            if (EditorManager.EditMode != EditMode.NONE) {
                return;
            }
            DraggableBButton btn = (DraggableBButton)sender;
            if (mMainWindow != null) {
                mMainWindow.BringToFront();
            }

            IconDynamicsHandle handle = btn.IconHandle;
            VsqEvent item = new VsqEvent();
            item.Clock = 0;
            item.ID.Note = 60;
            item.ID.type = VsqIDType.Aicon;
            item.ID.IconDynamicsHandle = (IconDynamicsHandle)handle.clone();
            int length = handle.getLength();
            if (length <= 0) {
                length = 1;
            }
            item.ID.setLength(length);
            EditorManager.mAddingEvent = item;

            btn.DoDragDrop(handle, Cadencii.Gui.Toolkit.DragDropEffects.All);
        }
        #endregion

        #region UI implementation
        private void InitializeComponent()
        {
            this.SuspendLayout();
			ApplicationUIHost.Instance.ApplyXml (this, "FormIconPaletteUi.xml");
            this.ResumeLayout(false);
            this.PerformLayout();

        }

		#pragma warning disable 0169,0649
        UiMenuStrip menuBar;
        UiToolStripMenuItem menuWindow;
        UiToolStripMenuItem menuWindowHide;
		#pragma warning restore 0169,0649

        #endregion

    }

}