/*
 * FormVibratoPreset.cs
 * Copyright © 2010 kbinani
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
using cadencii.apputil;
using Cadencii.Media.Vsq;
using cadencii;
using cadencii.java.util;

using Cadencii.Gui;
using Cadencii.Utilities;
using Cadencii.Gui.Toolkit;
using Cadencii.Application.Controls;
using Cadencii.Application.Drawing;


namespace Cadencii.Application.Forms
{

    public class FormVibratoPresetImpl : FormImpl
    {
        /// <summary>
        /// プレビューの各グラフにおいて，上下に追加するマージンの高さ(ピクセル)
        /// </summary>
        private const int MARGIN = 3;
        /// <summary>
        /// 折れ線の描画時に，描画するかどうかを決める閾値
        /// </summary>
        private const int MIN_DELTA = 2;
        /// <summary>
        /// 前回サイズ変更時の，フォームの幅
        /// </summary>
        private static int mPreviousWidth = 527;
        /// <summary>
        /// 前回サイズ変更時の，フォームの高さ
        /// </summary>
        private static int mPreviousHeight = 418;

        /// <summary>
        /// EditorManager.editorConfig.AutoVibratoCustomからコピーしてきた，
        /// ビブラートハンドルのリスト
        /// </summary>
        private List<VibratoHandle> mHandles;
        /// <summary>
        /// 選択状態のビブラートハンドル
        /// </summary>
        private VibratoHandle mSelected = null;
        /// <summary>
        /// Rateカーブを描画するのに使う描画器
        /// </summary>
        private LineGraphDrawer mDrawerRate = null;
        /// <summary>
        /// Depthカーブを描画するのに使う描画器
        /// </summary>
        private LineGraphDrawer mDrawerDepth = null;
        /// <summary>
        /// 結果として得られるピッチベンドカーブを描画するのに使う描画器
        /// </summary>
        private LineGraphDrawer mDrawerResulting = null;

        /// <summary>
        /// コンストラクタ．
        /// </summary>
        /// <param name="handles"></param>
        public FormVibratoPresetImpl(List<VibratoHandle> handles)
        {
            InitializeComponent();
            applyLanguage();
            AwtHost.Current.ApplyFontRecurse(this, EditorManager.editorConfig.getBaseFont());
            this.Size = new System.Drawing.Size(mPreviousWidth, mPreviousHeight);
            registerEventHandlers();

            // ハンドルのリストをクローン
            mHandles = new List<VibratoHandle>();
            int size = handles.Count;
            for (int i = 0; i < size; i++) {
                mHandles.Add((VibratoHandle)handles[i].clone());
            }

            // 表示状態を更新
            updateStatus();
            if (size > 0) {
                listPresets.SelectedIndex = 0;
            }
        }

        #region public methods
        /// <summary>
        /// ダイアログによる設定結果を取得します
        /// </summary>
        /// <returns></returns>
        public List<VibratoHandle> getResult()
        {
            // iconIDを整える
            if (mHandles == null) {
                mHandles = new List<VibratoHandle>();
            }
            int size = mHandles.Count;
            for (int i = 0; i < size; i++) {
                mHandles[i].IconID = "$0404" + PortUtil.toHexString(i + 1, 4);
            }
            return mHandles;
        }
        #endregion

        #region event handlers
        public void buttonOk_Click(Object sender, EventArgs e)
        {
			this.AsAwt ().DialogResult = Cadencii.Gui.DialogResult.OK;
        }

        public void buttonCancel_Click(Object sender, EventArgs e)
        {
			this.AsAwt ().DialogResult = Cadencii.Gui.DialogResult.Cancel;
        }

        public void listPresets_SelectedIndexChanged(Object sender, EventArgs e)
        {
            // インデックスを取得
            int index = listPresets.SelectedIndex;
#if DEBUG
            Logger.StdOut("FormVibratoPreset#listPresets_SelectedIndexChanged; index=" + index);
#endif

            // 範囲外ならbailout
            if ((index < 0) || (mHandles.Count <= index)) {
#if DEBUG
                Logger.StdOut("FormVibratoPreset#listPresets_SelectedIndexChanged; bail-out, mSelected -> null; index=" + index);
#endif
                mSelected = null;
                return;
            }

            // イベントハンドラを一時的に取り除く
            textDepth.TextChanged -= new EventHandler(textDepth_TextChanged);
            textRate.TextChanged -= new EventHandler(textRate_TextChanged);
            textName.TextChanged -= new EventHandler(textName_TextChanged);

            // テクストボックスに値を反映
            mSelected = mHandles[index];
            textDepth.Text = mSelected.getStartDepth() + "";
            textRate.Text = mSelected.getStartRate() + "";
            textName.Text = mSelected.getCaption();

            // イベントハンドラを再登録
            textDepth.TextChanged += new EventHandler(textDepth_TextChanged);
            textRate.TextChanged += new EventHandler(textRate_TextChanged);
            textName.TextChanged += new EventHandler(textName_TextChanged);

            // 再描画
            repaintPictures();
        }

        public void textName_TextChanged(Object sender, EventArgs e)
        {
            if (mSelected == null) {
                return;
            }

            mSelected.setCaption(textName.Text);
            int index = listPresets.SelectedIndex;
            if (index >= 0) {
                listPresets.Items[index] = mSelected.getCaption();
            }
        }

        public void textRate_TextChanged(Object sender, EventArgs e)
        {
            if (mSelected == null) {
                return;
            }

            int old = mSelected.getStartRate();
            int value = old;
            string s = textRate.Text;
            try {
                value = int.Parse(s);
            } catch (Exception ex) {
                value = old;
            }
            if (value < 0) {
                value = 0;
            }
            if (127 < value) {
                value = 127;
            }
            mSelected.setStartRate(value);
            string nstr = value + "";
            if (s != nstr) {
                textRate.Text = nstr;
                textRate.SelectionStart = textRate.Text.Length;
            }

            repaintPictures();
        }

        public void textDepth_TextChanged(Object sender, EventArgs e)
        {
            if (mSelected == null) {
                return;
            }

            int old = mSelected.getStartDepth();
            int value = old;
            string s = textDepth.Text;
            try {
                value = int.Parse(s);
            } catch (Exception ex) {
                value = old;
            }
            if (value < 0) {
                value = 0;
            }
            if (127 < value) {
                value = 127;
            }
            mSelected.setStartDepth(value);
            string nstr = value + "";
            if (s != nstr) {
                textDepth.Text = nstr;
                textDepth.SelectionStart = textDepth.Text.Length;
            }

            repaintPictures();
        }

        public void buttonAdd_Click(Object sender, EventArgs e)
        {
            // 追加し，
            VibratoHandle handle = new VibratoHandle();
            handle.setCaption("No-Name");
            mHandles.Add(handle);
            listPresets.SelectedIndices.Clear();
            // 表示反映させて
            updateStatus();
            // 追加したのを選択状態にする
            listPresets.SelectedIndex = mHandles.Count - 1;
        }

        public void buttonRemove_Click(Object sender, EventArgs e)
        {
            int index = listPresets.SelectedIndex;
            if (index < 0 || listPresets.Items.Count <= index) {
                return;
            }

            mHandles.RemoveAt(index);
            updateStatus();
        }

        public void handleUpDownButtonClick(Object sender, EventArgs e)
        {
            // 送信元のボタンによって，選択インデックスの増分を変える
            int delta = 1;
            if (sender == buttonUp) {
                delta = -1;
            }

            // 移動後のインデックスは？
            int index = listPresets.SelectedIndex;
            int move_to = index + delta;

            // 範囲内かどうか
            if (index < 0) {
                return;
            }
            if (move_to < 0 || mHandles.Count <= move_to) {
                // 範囲外なら何もしない
                return;
            }

            // 入れ替える
            VibratoHandle buff = mHandles[index];
            mHandles[index] = mHandles[move_to];
            mHandles[move_to] = buff;

            // 選択状態を変える
            listPresets.SelectedIndices.Clear();
            updateStatus();
            listPresets.SelectedIndex = move_to;
        }

        public void pictureResulting_Paint(Object sender, Cadencii.Gui.Toolkit.PaintEventArgs e)
        {
            // 背景を描画
            int raw_width = pictureResulting.Width;
            int raw_height = pictureResulting.Height;
			var g = (System.Drawing.Graphics) e.Graphics.NativeGraphics;
            g.FillRectangle(System.Drawing.Brushes.LightGray, 0, 0, raw_width, raw_height);

            // 選択中のハンドルを取得
            VibratoHandle handle = mSelected;
            if (handle == null) {
                return;
            }

            // 描画の準備
            LineGraphDrawer d = getDrawerResulting();
			d.setGraphics(new Cadencii.Gui.Graphics () { NativeGraphics = g});

            // ビブラートのピッチベンドを取得するイテレータを取得
            int width = raw_width;
            int vib_length = 960;
            int tempo = 500000;
            double vib_seconds = tempo * 1e-6 / 480.0 * vib_length;
            // 480クロックは0.5秒
            VsqFileEx vsq = new VsqFileEx("Miku", 1, 4, 4, tempo);
            VibratoBPList list_rate = handle.getRateBP();
            VibratoBPList list_depth = handle.getDepthBP();
            int start_rate = handle.getStartRate();
            int start_depth = handle.getStartDepth();
            if (list_rate == null) {
                list_rate = new VibratoBPList(new float[] { 0.0f }, new int[] { start_rate });
            }
            if (list_depth == null) {
                list_depth = new VibratoBPList(new float[] { 0.0f }, new int[] { start_depth });
            }
            // 解像度
            float resol = (float)(vib_seconds / width);
            if (resol <= 0.0f) {
                return;
            }
            VibratoPointIteratorBySec itr =
                new VibratoPointIteratorBySec(
                    vsq,
                    list_rate, start_rate,
                    list_depth, start_depth,
                    0, vib_length, resol);

            // 描画
            int height = raw_height - MARGIN * 2;
            d.clear();
            //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int x = 0;
            int lastx = 0;
            int lasty = -10;
            int tx = 0, ty = 0;
            for (; itr.hasNext(); x++) {
                double pitch = itr.next().getY();
                int y = height - (int)((pitch + 1.25) / 2.5 * height) + MARGIN - 1;
                int dx = x - lastx; // xは単調増加
                int dy = Math.Abs(y - lasty);
                tx = x;
                ty = y;
                //if ( dx > MIN_DELTA || dy > MIN_DELTA ) {
                d.append(x, y);
                lastx = x;
                lasty = y;
                //}
            }
            d.append(tx, ty);
            d.flush();
        }

        public void pictureRate_Paint(Object sender, Cadencii.Gui.Toolkit.PaintEventArgs e)
        {
            // 背景を描画
            int width = pictureRate.Width;
            int height = pictureRate.Height;
			var g = (System.Drawing.Graphics) e.Graphics.NativeGraphics;
            g.FillRectangle(System.Drawing.Brushes.LightGray, 0, 0, width, height);

            // 選択中のハンドルを取得
            VibratoHandle handle = mSelected;
            if (handle == null) {
                return;
            }

            // 描画の準備
            LineGraphDrawer d = getDrawerRate();
            d.clear();
			d.setGraphics(new Cadencii.Gui.Graphics () { NativeGraphics = g});
            drawVibratoCurve(
                handle.getRateBP(),
                handle.getStartRate(),
                d,
                width, height);
        }

        public void pictureDepth_Paint(Object sender, Cadencii.Gui.Toolkit.PaintEventArgs e)
        {
            // 背景を描画
            int width = pictureDepth.Width;
            int height = pictureDepth.Height;
			var g = (System.Drawing.Graphics) e.Graphics.NativeGraphics;
            g.FillRectangle(System.Drawing.Brushes.LightGray, 0, 0, width, height);

            // 選択中のハンドルを取得
            VibratoHandle handle = mSelected;
            if (handle == null) {
                return;
            }

            // 描画の準備
            LineGraphDrawer d = getDrawerDepth();
            d.clear();
            d.setGraphics(new Cadencii.Gui.Graphics () { NativeGraphics = g});
            drawVibratoCurve(
                handle.getDepthBP(),
                handle.getStartDepth(),
                d,
                width, height);
        }

        public void FormVibratoPreset_Resize(Object sender, EventArgs e)
        {
			if (this.AsAwt ().WindowState == FormWindowState.Normal) {
                mPreviousWidth = this.Width;
                mPreviousHeight = this.Height;
            }
            repaintPictures();
        }
        #endregion

        #region helper methods
        /// <summary>
        /// イベントハンドラを登録します
        /// </summary>
        private void registerEventHandlers()
        {
            listPresets.SelectedIndexChanged += new EventHandler(listPresets_SelectedIndexChanged);
            textDepth.TextChanged += new EventHandler(textDepth_TextChanged);
            textRate.TextChanged += new EventHandler(textRate_TextChanged);
            textName.TextChanged += new EventHandler(textName_TextChanged);
            buttonAdd.Click += new EventHandler(buttonAdd_Click);
            buttonRemove.Click += new EventHandler(buttonRemove_Click);
            buttonUp.Click += new EventHandler(handleUpDownButtonClick);
            buttonDown.Click += new EventHandler(handleUpDownButtonClick);

			pictureDepth.Paint += pictureDepth_Paint;
			pictureRate.Paint += pictureRate_Paint;
			pictureResulting.Paint += pictureResulting_Paint;

            this.Resize += new EventHandler(FormVibratoPreset_Resize);
            buttonOk.Click += new EventHandler(buttonOk_Click);
            buttonCancel.Click += new EventHandler(buttonCancel_Click);
        }

        private static string _(string id)
        {
            return Messaging.getMessage(id);
        }

        private void applyLanguage()
        {
            this.Text = _("Vibrato preset");

            labelPresets.Text = _("List of vibrato preset");

            groupEdit.Text = _("Edit");
            labelName.Text = _("Name");

            groupPreview.Text = _("Preview");
            labelDepthCurve.Text = _("Depth curve");
            labelRateCurve.Text = _("Rate curve");
            labelResulting.Text = _("Resulting pitch bend");

            buttonAdd.Text = _("Add");
            buttonRemove.Text = _("Remove");
            buttonUp.Text = _("Up");
            buttonDown.Text = _("Down");

            buttonOk.Text = _("OK");
            buttonCancel.Text = _("Cancel");
        }

        /// <summary>
        /// Rate, Depth, Resulting pitchの各グラフを強制描画します
        /// </summary>
        private void repaintPictures()
        {
            pictureDepth.Refresh();
            pictureRate.Refresh();
            pictureResulting.Refresh();
        }

        /// <summary>
        /// ビブラートのRateまたはDepthカーブを指定したサイズで描画します
        /// </summary>
        /// <param name="list">描画するカーブ</param>
        /// <param name="start_value"></param>
        /// <param name="drawer"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void drawVibratoCurve(VibratoBPList list, int start_value, LineGraphDrawer drawer, int width, int height)
        {
            int size = 0;
            if (list != null) {
                size = list.getCount();
            }
            drawer.clear();
            drawer.setBaseLineY(height);
            int iy0 = height - (int)(start_value / 127.0 * height);
            drawer.append(0, iy0);
            int lasty = iy0;
            for (int i = 0; i < size; i++) {
                VibratoBPPair p = list.getElement(i);
                int ix = (int)(p.X * width);
                int iy = height - (int)(p.Y / 127.0 * height);
                drawer.append(ix, iy);
                lasty = iy;
            }
            drawer.append(width + drawer.getDotSize() * 2, lasty);
            drawer.flush();
        }

        /// <summary>
        /// Rateカーブを描画するのに使う描画器を取得します
        /// </summary>
        /// <returns></returns>
        private LineGraphDrawer getDrawerRate()
        {
            if (mDrawerRate == null) {
                mDrawerRate = new LineGraphDrawer(LineGraphDrawer.TYPE_STEP);
                mDrawerRate.setDotMode(LineGraphDrawer.DOTMODE_ALWAYS);
                mDrawerRate.setFillColor(Cadencii.Gui.Colors.CornflowerBlue);
            }
            return mDrawerRate;
        }

        /// <summary>
        /// Depthカーブを描画するのに使う描画器を取得します
        /// </summary>
        /// <returns></returns>
        private LineGraphDrawer getDrawerDepth()
        {
            if (mDrawerDepth == null) {
                mDrawerDepth = new LineGraphDrawer(LineGraphDrawer.TYPE_STEP);
                mDrawerDepth.setDotMode(LineGraphDrawer.DOTMODE_ALWAYS);
				mDrawerDepth.setFillColor(Cadencii.Gui.Colors.CornflowerBlue);
            }
            return mDrawerDepth;
        }

        /// <summary>
        /// 結果として得られるピッチベンドカーブを描画するのに使う描画器を取得します
        /// </summary>
        /// <returns></returns>
        private LineGraphDrawer getDrawerResulting()
        {
            if (mDrawerResulting == null) {
                mDrawerResulting = new LineGraphDrawer(LineGraphDrawer.TYPE_LINEAR);
                mDrawerResulting.setDotMode(LineGraphDrawer.DOTMODE_NO);
                mDrawerResulting.setFill(false);
                mDrawerResulting.setLineWidth(2);
				mDrawerResulting.setLineColor(Cadencii.Gui.Colors.ForestGreen);
            }
            return mDrawerResulting;
        }

        /// <summary>
        /// 画面の表示状態を更新します
        /// </summary>
        private void updateStatus()
        {
            int old_select = listPresets.SelectedIndex;
            listPresets.SelectedIndices.Clear();

            // アイテムの個数に過不足があれば数を整える
            int size = mHandles.Count;
            int delta = size - listPresets.Items.Count;
#if DEBUG
            Logger.StdOut("FormVibratoPreset#updateStatus; delta=" + delta);
#endif
            if (delta > 0) {
                for (int i = 0; i < delta; i++) {
                    listPresets.Items.Add("");
                }
            } else if (delta < 0) {
                for (int i = 0; i < -delta; i++) {
                    listPresets.Items.RemoveAt(0);
                }
            }

            // アイテムを更新
            for (int i = 0; i < size; i++) {
                VibratoHandle handle = mHandles[i];
                listPresets.Items[i] = handle.getCaption();
            }

            // 選択状態を復帰
            if (size <= old_select) {
                old_select = size - 1;
            }
#if DEBUG
            Logger.StdOut("FormVibratoPreset#updateStatus; A; old_selected=" + old_select);
#endif
            if (old_select >= 0) {
#if DEBUG
                Logger.StdOut("FormVibratoPreset#updateStatus; B; old_selected=" + old_select);
#endif
                listPresets.SelectedIndex = old_select;
            }
        }
        #endregion

        #region UI Impl for C#
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
            this.SuspendLayout();
			ApplicationUIHost.Instance.ApplyXml (this, "FormVibratoPreset.xml");
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

		#pragma warning disable 0649
        UiButton buttonCancel;
        UiButton buttonOk;
        UiButton buttonRemove;
        UiButton buttonAdd;
        UiButton buttonUp;
        UiLabel labelRate;
        UiLabel labelDepth;
        NumberTextBox textRate;
        NumberTextBox textDepth;
        UiLabel labelPresets;
        UiPictureBox pictureRate;
        UiLabel labelRateCurve;
        UiLabel labelDepthCurve;
        UiPictureBox pictureDepth;
        UiSplitContainer splitContainer1;
        UiSplitContainer splitContainer2;
        UiLabel labelResulting;
        UiPictureBox pictureResulting;
        UiGroupBox groupEdit;
        UiLabel labelName;
        UiTextBox textName;
        UiGroupBox groupPreview;
        UiListBox listPresets;
        UiButton buttonDown;
		#pragma warning restore 0649
        #endregion
    }

}
