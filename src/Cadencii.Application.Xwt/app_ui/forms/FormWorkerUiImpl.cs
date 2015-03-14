/*
 * FormWorkerUi.cs
 * Copyright © 2011 kbinani
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using cadencii.apputil;
using Cadencii.Gui;
using Cadencii.Gui.Toolkit;
using Cadencii.Application.Controls;
using cadencii;
using Cadencii.Utilities;
using System.Threading;



namespace Cadencii.Application.Forms
{
    public class FormWorkerUiImpl : FormImpl, FormWorkerUi
    {
		bool FormWorkerUi.showDialogTo (UiForm formMainWindow)
		{
			return showDialogTo ((FormMainImpl) formMainWindow);
		}

        UiProgressBar progressBar1;
        UiFlowLayoutPanel flowLayoutPanel1;
        UiLabel label1;
        IFormWorkerControl mControl;
        UiButton buttonCancel;
        UiButton buttonDetail;
        private bool mDetailVisible = true;
        private int mFullHeight = 1;

        private delegate void DelegateArgIntReturnVoid(int i0);
        private delegate void DelegateArgStringReturnVoid(string s0);

        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public FormWorkerUiImpl(IFormWorkerControl control)
        {
            InitializeComponent();
			registerEventHandlers ();
            mControl = control;
			mFullHeight = this.AsGui ().Height;
        }

        private static string _(string id)
        {
            return Messaging.getMessage(id);
        }

        public void applyLanguage()
        {
            buttonCancel.Text = _("Cancel");
            buttonDetail.Text = _("detail");
        }

        /// <summary>
        /// フォームを閉じます
        /// </summary>
        public void close()
        {
			this.AsGui ().DialogResult = Cadencii.Gui.DialogResult.OK;
			this.AsGui ().Close();
        }

        /// <summary>
        /// 全体の進捗状況の表示を更新します．
        /// </summary>
        /// <param name="percentage"></param>
        public void setTotalProgress(int percentage)
        {
			SynchronizationContext.Current.Send (o => setTotalProgressUnsafe (percentage), null);
        }

        /// <summary>
        /// 追加されたプログレスバーをこのフォームから削除します
        /// </summary>
        /// <param name="ui"></param>
        public void removeProgressBar(ProgressBarWithLabel ui)
        {
			flowLayoutPanel1.RemoveControl(ui);
        }

        /// <summary>
        /// プログレスバーをこのフォームに追加します．
        /// </summary>
        /// <param name="ui"></param>
        public void addProgressBar(ProgressBarWithLabel ui)
        {
			// FIXME: not sure if it works (commented WF-dependent code)
			int draft_width = flowLayoutPanel1.Width - 10;// - ((GuiHostWindowsForms) GuiHost.Current).VerticalScrollBarWidth;
            if (draft_width < 1) {
                draft_width = 1;
            }
            ui.Width = draft_width;
            //ui.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            //ui.Dock = DockStyle.Top;
            flowLayoutPanel1.AddControl(ui);
        }

        /// <summary>
        /// フォームのタイトルを設定します
        /// </summary>
        /// <param name="p"></param>
        public void setTitle(string p)
        {
			SynchronizationContext.Current.Send (o => setTitleUnsafe (p), null);
        }

        /// <summary>
        /// フォームのメッセージテキストを設定します
        /// </summary>
        /// <param name="p"></param>
        public void setText(string p)
        {
			SynchronizationContext.Current.Send (o => setTextUnsafe (p), null);
        }

        private void setTitleUnsafe(string value)
        {
			this.AsGui ().Text = value;
        }

        private void setTextUnsafe(string value)
        {
            label1.Text = value;
        }

        private void setTotalProgressUnsafe(int percentage)
        {
            if (percentage < progressBar1.Minimum) percentage = progressBar1.Minimum;
            if (progressBar1.Maximum < percentage) percentage = progressBar1.Maximum;
            progressBar1.Value = percentage;
        }

        /// <summary>
        /// このフォームを指定したウィンドウに対してモーダルに表示します．
        /// フォームがキャンセルされた場合true，そうでない場合はfalseを返します
        /// </summary>
        /// <param name="main_window"></param>
        /// <returns></returns>
        public bool showDialogTo(FormMainImpl main_window)
        {
			if (AsGui ().ShowDialog (main_window) == Cadencii.Gui.DialogResult.Cancel) {
                return true;
            } else {
                return false;
            }
        }

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
			ApplicationUIHost.Instance.ApplyXml (this, "FormWorkerUi.xml");
        }

		void registerEventHandlers ()
		{
			this.buttonCancel.Click += new System.EventHandler(buttonCancel_Click);
			this.buttonDetail.Click += new System.EventHandler (buttonDetail_Click);
			this.ParentWindow.BoundsChanged += FormWorkerUi_SizeChanged;
			this.ParentWindow.CloseRequested += FormWorkerUi_FormClosing;
		}

        #endregion

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            mControl.cancelJobSlot();
        }

		private void FormWorkerUi_FormClosing(object sender, Xwt.CloseRequestedEventArgs e)
        {
            mControl.cancelJobSlot();
        }

        private void FormWorkerUi_SizeChanged(object sender, EventArgs e)
        {
            if (flowLayoutPanel1.Visible) {
				mFullHeight = (int) this.ParentWindow.Height;
				// FIXME: not sure if this works (commented out WF-dependent code)
				int draft_width = flowLayoutPanel1.Width - 10;// - ((GuiHostWindowsForms) GuiHost.Current).VerticalScrollBarWidth;
                if (draft_width < 1) {
                    draft_width = 1;
                }
                foreach (UiControl c in flowLayoutPanel1.Controls) {
                    c.Width = draft_width;
                }
            }
        }

        private void buttonDetail_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = !flowLayoutPanel1.Visible;
            if (flowLayoutPanel1.Visible) {
                this.ParentWindow.Height = mFullHeight;
            } else {
				int w = (int) this.ParentWindow.Size.Width;
                int delta = flowLayoutPanel1.Top - buttonCancel.Bottom;
                int h = buttonCancel.Bottom + delta - 2;
				this.AsGui ().ClientSize = new Size(w, h);
            }
        }
    }
}
