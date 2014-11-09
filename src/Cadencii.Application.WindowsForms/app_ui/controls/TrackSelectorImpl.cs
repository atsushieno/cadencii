/*
 * TrackSelector.cs
 * Copyright © 2008-2011 kbinani
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


#define COMPONENT_ENABLE_LOCATION
//#define MONITOR_FPS
//#define OLD_IMPL_MOUSE_TRACER
using System;
using System.Threading;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using cadencii;
using cadencii.apputil;
using Cadencii.Gui;
using cadencii.java.util;
using cadencii.vsq;

using cadencii.core;
using Cadencii.Utilities;

using Keys = Cadencii.Gui.Keys;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;
using MouseButtons = System.Windows.Forms.MouseButtons;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using MouseEventHandler = System.Windows.Forms.MouseEventHandler;
using NMouseButtons = Cadencii.Gui.MouseButtons;
using NMouseEventArgs = Cadencii.Gui.MouseEventArgs;
using NMouseEventHandler = Cadencii.Gui.MouseEventHandler;
using ToolStripRenderMode = Cadencii.Gui.ToolStripRenderMode;
using TS = cadencii.TrackSelectorConsts;

namespace cadencii
{
    using Graphics = Cadencii.Gui.Graphics;

    /// <summary>
    /// コントロールカーブ，トラックの一覧，歌手変更イベントなどを表示するコンポーネント．
    /// </summary>
    public class TrackSelectorImpl : UserControlImpl, TrackSelector
    {

		BezierPoint TrackSelector.HandleMouseMoveForBezierMove (Cadencii.Gui.MouseEventArgs e, BezierPickedSide picked)
		{
			return HandleMouseMoveForBezierMove (e.ToWF (), picked);
		}

		void TrackSelector.setEditingPointID (int id)
		{
			mEditingPointID = id;
		}

		void TrackSelector.onMouseDown (object sender, Cadencii.Gui.MouseEventArgs e)
		{
			onMouseDown (sender, e.ToWF ());
		}

		void TrackSelector.onMouseUp (object sender, Cadencii.Gui.MouseEventArgs e)
		{
			onMouseUp (sender, e.ToWF ());
		}


        /// <summary>
        /// 現在最前面に表示されているカーブ
        /// </summary>
        private CurveType mSelectedCurve = CurveType.VEL;
        /// <summary>
        /// 現在最前面カーブのすぐ後ろに表示されているカーブ
        /// </summary>
        private CurveType mLastSelectedCurve = CurveType.DYN;
        /// <summary>
        /// コントロールカーブを表示するモードかどうか
        /// </summary>
        private bool mCurveVisible = true;
        /// <summary>
        /// 現在のマウス位置におけるカーブの値
        /// </summary>
        private int mMouseValue;
        /// <summary>
        /// 編集しているBezierChainのID
        /// </summary>
        public int mEditingChainID = -1;
        /// <summary>
        /// 編集しているBezierPointのID
        /// </summary>
        public int mEditingPointID = -1;
        /// <summary>
        /// マウスがカーブ部分に下ろされている最中かどうかを表すフラグ
        /// </summary>
        private bool mMouseDowned = false;
        /// <summary>
        /// マウスのトレーサ。コントロールカーブ用の仮想スクリーン座標で表す。
        /// </summary>
        private MouseTracer mMouseTracer = new MouseTracer();
        private bool mPencilMoved = false;
        private Thread mMouseHoverThread = null;
        /// <summary>
        /// cmenuSingerのメニューアイテムを初期化するのに使用したRenderer。
        /// </summary>
        private RendererKind mCMenuSingerPrepared = RendererKind.NULL;
        /// <summary>
        /// マウスがDownしてからUpするまでのモード
        /// </summary>
        private MouseDownMode mMouseDownMode = MouseDownMode.NONE;
        /// <summary>
        /// マウスがDownしてからマウスが移動したかどうかを表す。
        /// </summary>
        private bool mMouseMoved = false;
        /// <summary>
        /// マウスドラッグで歌手変更イベントの矩形を移動開始した時の、マウス位置におけるクロック
        /// </summary>
        private int mSingerMoveStartedClock;
        /// <summary>
        /// cmenuSinger用のツールチップの幅を記録しておく。
        /// </summary>
        private int[] mCMenuSingerTooltipWidth;
        /// <summary>
        /// マウス長押しによるVELの編集。選択されている音符のInternalID
        /// </summary>
        private int mVelEditLastSelectedID = -1;
        /// <summary>
        /// マウス長押しによるVELの編集。棒グラフのてっぺんの座標と、マウスが降りた座標の差分。プラスなら、マウスの方が下になる。
        /// </summary>
        private int mVelEditShiftY = 0;
        /// <summary>
        /// マウス長押しによるVELの編集。編集対象の音符のリスト。
        /// </summary>
        private SortedDictionary<int, SelectedEventEntry> mVelEditSelected = new SortedDictionary<int, SelectedEventEntry>();
        /// <summary>
        /// 現在編集操作が行われているBezierChainの、編集直前のオリジナル
        /// </summary>
        private BezierChain mEditingBezierOriginal = null;
        /// <summary>
        /// CTRLキー。MacOSXの場合はMenu
        /// </summary>
        private Keys mModifierKey = Keys.Control;
        /// <summary>
        /// スペースキーが押されているかどうか。
        /// MouseDown時に範囲選択モードをスキップする必要があるので、FormMainでの処理に加えてこのクラス内部でも処理する必要がある
        /// </summary>
        private bool mSpaceKeyDowned = false;
        /// <summary>
        /// マウスがDownした位置の座標．xは仮想スクリーン座標．yは通常のe.Location.Y
        /// </summary>
        private Point mMouseDownLocation = new Point();
        /// <summary>
        /// エンベロープ点を動かすモードで，選択されているInternalID．
        /// </summary>
        private int mEnvelopeEdigintID = -1;
        /// <summary>
        /// エンベロープ点を動かすモードで，選択されている点のタイプ
        /// </summary>
        private int mEnvelopePointKind = -1;
        /// <summary>
        /// エンベロープ点を動かすモードで，編集される前のオリジナルのエンベロープ
        /// </summary>
        private UstEnvelope mEnvelopeOriginal = null;
        /// <summary>
        /// エンベロープ点を動かすモードで、点が移動可能な範囲の始点(秒)
        /// </summary>
        private double mEnvelopeDotBegin;
        /// <summary>
        /// エンベロープ点を動かすモードで、点が移動可能な範囲の終点(秒)
        /// </summary>
        private double mEnvelopeDotEnd;
        /// <summary>
        /// 編集中のエンベロープ
        /// </summary>
        private UstEnvelope mEnvelopeEditing = null;
        /// <summary>
        /// 編集中のエンベロープの範囲の始点（秒）
        /// </summary>
        private double mEnvelopeRangeBegin;
        /// <summary>
        /// 編集中のエンベロープの範囲の終点（秒）
        /// </summary>
        private double mEnvelopeRangeEnd;

        /// <summary>
        /// 現在PreUtteranceを編集中のVsqEventのID
        /// </summary>
        private int mPreUtteranceEditingID;

        /// <summary>
        /// 現在オーバーラップを編集中のVsqEventのID
        /// </summary>
        private int mOverlapEditingID;
        /// <summary>
        /// オーバーラップを編集する前の音符情報
        /// </summary>
        private VsqEvent mPreOverlapOriginal = null;
        /// <summary>
        /// オーバーラップを編集中の音符情報
        /// </summary>
        private VsqEvent mPreOverlapEditing = null;

        /// <summary>
        /// MouseDown時のControl.Modifiersの状態。
        /// </summary>
        private Keys mModifierOnMouseDown = Keys.None;
        /// <summary>
        /// 移動しているデータ点のリスト
        /// </summary>
        private List<BPPair> mMovingPoints = new List<BPPair>();
        /// <summary>
        /// このコントロールの推奨最少表示高さの前回の値．
        /// 推奨表示高さが変わったかどうかを検出するのに使う
        /// </summary>
        private int mLastPreferredMinHeight;
        /// <summary>
        /// 描画幅が2ピクセルのストローク
        /// </summary>
        private Stroke mStroke2px = null;
        /// <summary>
        /// デフォルトのストローク
        /// </summary>
        private Stroke mStrokeDefault = null;
        /// <summary>
        /// 折れ線グラフを効率よく描画するための描画器
        /// </summary>
        private LineGraphDrawer mGraphDrawer = null;
        private Graphics mGraphics = null;
        /// <summary>
        /// メイン画面への参照
        /// </summary>
        private FormMainImpl mMainWindow = null;
        /// <summary>
        /// Overlap, Presendを描画するときに使うフォントで，一文字あたり何ピクセルになるか
        /// </summary>
        private float mTextWidthPerLetter = 0.0f;
        /// <summary>
        /// Overlap, Presendを描画するときに使うフォントの，文字の描画高さ
        /// </summary>
        private int mTextHeight = 0;
        /// <summary>
        /// カーブ種類とメニューアイテムを紐付けるマップ
        /// </summary>
        private SortedDictionary<CurveType, UiToolStripMenuItem> mMenuMap = new SortedDictionary<CurveType, UiToolStripMenuItem>();
        /// <summary>
        /// ツールチップに表示されるプログラム
        /// </summary>
        private int mTooltipProgram;
        /// <summary>
        /// ツールチップに表示されるLanguage
        /// </summary>
        private int mTooltipLanguage;
        /// <summary>
        /// TrackSelectorで表示させているカーブの一覧
        /// </summary>
        private List<CurveType> mViewingCurves = new List<CurveType>();

        /// <summary>
        /// 最前面に表示するカーブの種類が変更されたとき発生するイベント．
        /// </summary>
        public event SelectedCurveChangedEventHandler SelectedCurveChanged;
        /// <summary>
        /// 表示するトラック番号が変更されたとき発生するイベント．
        /// </summary>
        public event SelectedTrackChangedEventHandler SelectedTrackChanged;
        /// <summary>
        /// VSQの編集コマンドが発行されたとき発生するイベント．
        /// </summary>
        public event EventHandler CommandExecuted;
        /// <summary>
        /// トラックの歌声合成が要求されたとき発生するイベント．
        /// </summary>
        public event RenderRequiredEventHandler RenderRequired;
        /// <summary>
        /// このコントロールの推奨最少表示高さが変わったとき発生するイベント．
        /// </summary>
        public event EventHandler PreferredMinHeightChanged;

        /// <summary>
        /// コンストラクタ．
        /// </summary>
        public TrackSelectorImpl(FormMainImpl main_window)
        {
            this.SetStyle(System.Windows.Forms.ControlStyles.DoubleBuffer, true);
            this.SetStyle(System.Windows.Forms.ControlStyles.UserPaint, true);
            InitializeComponent();
            mMainWindow = main_window;
            registerEventHandlers();
            setResources();
            mModifierKey = Keys.Control;
            mMenuMap[CurveType.VEL] = cmenuCurveVelocity;
            mMenuMap[CurveType.Accent] = cmenuCurveAccent;
            mMenuMap[CurveType.Decay] = cmenuCurveDecay;

            mMenuMap[CurveType.DYN] = cmenuCurveDynamics;
            mMenuMap[CurveType.VibratoRate] = cmenuCurveVibratoRate;
            mMenuMap[CurveType.VibratoDepth] = cmenuCurveVibratoDepth;

            mMenuMap[CurveType.Reso1Amp] = cmenuCurveReso1Amp;
            mMenuMap[CurveType.Reso1Bw] = cmenuCurveReso1BW;
            mMenuMap[CurveType.Reso1Freq] = cmenuCurveReso1Freq;
            mMenuMap[CurveType.Reso2Amp] = cmenuCurveReso2Amp;
            mMenuMap[CurveType.Reso2Bw] = cmenuCurveReso2BW;
            mMenuMap[CurveType.Reso2Freq] = cmenuCurveReso2Freq;
            mMenuMap[CurveType.Reso3Amp] = cmenuCurveReso3Amp;
            mMenuMap[CurveType.Reso3Bw] = cmenuCurveReso3BW;
            mMenuMap[CurveType.Reso3Freq] = cmenuCurveReso3Freq;
            mMenuMap[CurveType.Reso4Amp] = cmenuCurveReso4Amp;
            mMenuMap[CurveType.Reso4Bw] = cmenuCurveReso4BW;
            mMenuMap[CurveType.Reso4Freq] = cmenuCurveReso4Freq;

            mMenuMap[CurveType.Harmonics] = cmenuCurveHarmonics;
            mMenuMap[CurveType.BRE] = cmenuCurveBreathiness;
            mMenuMap[CurveType.BRI] = cmenuCurveBrightness;
            mMenuMap[CurveType.CLE] = cmenuCurveClearness;
            mMenuMap[CurveType.OPE] = cmenuCurveOpening;
            mMenuMap[CurveType.GEN] = cmenuCurveGenderFactor;

            mMenuMap[CurveType.POR] = cmenuCurvePortamentoTiming;
            mMenuMap[CurveType.PIT] = cmenuCurvePitchBend;
            mMenuMap[CurveType.PBS] = cmenuCurvePitchBendSensitivity;

            mMenuMap[CurveType.Fx2Depth] = cmenuCurveEffect2Depth;
            mMenuMap[CurveType.Env] = cmenuCurveEnvelope;
        }

        /// <summary>
        /// 表示するコントロールのカーブの種類を、EditorManager.EditorConfigの設定に応じて更新します
        /// </summary>
        public void updateVisibleCurves()
        {
            mViewingCurves.Clear();
            if (ApplicationGlobal.appConfig.CurveVisibleVelocity) {
                mViewingCurves.Add(CurveType.VEL);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleAccent) {
                mViewingCurves.Add(CurveType.Accent);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleDecay) {
                mViewingCurves.Add(CurveType.Decay);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleEnvelope) {
                mViewingCurves.Add(CurveType.Env);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleDynamics) {
                mViewingCurves.Add(CurveType.DYN);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleBreathiness) {
                mViewingCurves.Add(CurveType.BRE);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleBrightness) {
                mViewingCurves.Add(CurveType.BRI);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleClearness) {
                mViewingCurves.Add(CurveType.CLE);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleOpening) {
                mViewingCurves.Add(CurveType.OPE);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleGendorfactor) {
                mViewingCurves.Add(CurveType.GEN);
            }
            if (ApplicationGlobal.appConfig.CurveVisiblePortamento) {
                mViewingCurves.Add(CurveType.POR);
            }
            if (ApplicationGlobal.appConfig.CurveVisiblePit) {
                mViewingCurves.Add(CurveType.PIT);
            }
            if (ApplicationGlobal.appConfig.CurveVisiblePbs) {
                mViewingCurves.Add(CurveType.PBS);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleVibratoRate) {
                mViewingCurves.Add(CurveType.VibratoRate);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleVibratoDepth) {
                mViewingCurves.Add(CurveType.VibratoDepth);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleHarmonics) {
                mViewingCurves.Add(CurveType.Harmonics);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleFx2Depth) {
                mViewingCurves.Add(CurveType.Fx2Depth);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleReso1) {
                mViewingCurves.Add(CurveType.Reso1Freq);
                mViewingCurves.Add(CurveType.Reso1Bw);
                mViewingCurves.Add(CurveType.Reso1Amp);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleReso2) {
                mViewingCurves.Add(CurveType.Reso2Freq);
                mViewingCurves.Add(CurveType.Reso2Bw);
                mViewingCurves.Add(CurveType.Reso2Amp);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleReso3) {
                mViewingCurves.Add(CurveType.Reso3Freq);
                mViewingCurves.Add(CurveType.Reso3Bw);
                mViewingCurves.Add(CurveType.Reso3Amp);
            }
            if (ApplicationGlobal.appConfig.CurveVisibleReso4) {
                mViewingCurves.Add(CurveType.Reso4Freq);
                mViewingCurves.Add(CurveType.Reso4Bw);
                mViewingCurves.Add(CurveType.Reso4Amp);
            }
        }

        /// <summary>
        /// メニューアイテムから，そのアイテムが担当しているカーブ種類を取得します
        /// </summary>
        private CurveType getCurveTypeFromMenu(UiToolStripMenuItem menu)
        {
            foreach (var curve in mMenuMap.Keys) {
                var search = mMenuMap[curve];
                if (menu == search) {
                    return curve;
                }
            }
            return CurveType.Empty;
        }

        #region Cadencii.Gui.Component
        public void setBounds(int x, int y, int width, int height)
        {
            base.Bounds = new System.Drawing.Rectangle(x, y, width, height);
        }

        public void setBounds(Cadencii.Gui.Rectangle rc)
        {
            base.Bounds = new System.Drawing.Rectangle(rc.X, rc.Y, rc.Width, rc.Height);
        }

        public Cadencii.Gui.Cursor getCursor()
        {
            System.Windows.Forms.Cursor c = base.Cursor;
            Cadencii.Gui.Cursor ret = null;
            if (c.Equals(System.Windows.Forms.Cursors.Arrow)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.DEFAULT_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.Cross)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.CROSSHAIR_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.Default)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.DEFAULT_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.Hand)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.HAND_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.IBeam)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.TEXT_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.PanEast)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.E_RESIZE_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.PanNE)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.NE_RESIZE_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.PanNorth)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.N_RESIZE_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.PanNW)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.NW_RESIZE_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.PanSE)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.SE_RESIZE_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.PanSouth)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.S_RESIZE_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.PanSW)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.SW_RESIZE_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.PanWest)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.W_RESIZE_CURSOR);
            } else if (c.Equals(System.Windows.Forms.Cursors.SizeAll)) {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.MOVE_CURSOR);
            } else {
                ret = new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.CUSTOM_CURSOR);
            }
            ret.NativeCursor = c;
            return ret;
        }

        public void setCursor(Cadencii.Gui.Cursor value)
        {
            base.Cursor = (System.Windows.Forms.Cursor) value.NativeCursor;
        }

#if COMPONENT_ENABLE_TOOL_TIP_TEXT
        public void setToolTipText( String value )
        {
            base.ToolTipText = value;
        }

        public String getToolTipText()
        {
            return base.ToolTipText;
        }
#endif

#if COMPONENT_PARENT_AS_OWNERITEM
        public Object getParent() {
            return base.OwnerItem;
        }
#else
        public Object getParent()
        {
            return base.Parent;
        }
#endif

        public string getName()
        {
            return base.Name;
        }

        public void setName(string value)
        {
            base.Name = value;
        }

#if COMPONENT_ENABLE_LOCATION
        public Cadencii.Gui.Point getLocationOnScreen()
        {
            System.Drawing.Point p = base.PointToScreen(base.Location);
            return new Cadencii.Gui.Point(p.X, p.Y);
        }

        public Cadencii.Gui.Point getLocation()
        {
            System.Drawing.Point loc = this.Location;
            return new Cadencii.Gui.Point(loc.X, loc.Y);
        }

        public void setLocation(int x, int y)
        {
            base.Location = new System.Drawing.Point(x, y);
        }

        public void setLocation(Cadencii.Gui.Point p)
        {
            base.Location = new System.Drawing.Point(p.X, p.Y);
        }
#endif

        public Cadencii.Gui.Rectangle getBounds()
        {
            System.Drawing.Rectangle r = base.Bounds;
            return new Cadencii.Gui.Rectangle(r.X, r.Y, r.Width, r.Height);
        }

#if COMPONENT_ENABLE_X
        public int getX() {
            return base.Left;
        }
#endif

#if COMPONENT_ENABLE_Y
        public int getY() {
            return base.Top;
        }
#endif

        public int getWidth()
        {
            return base.Width;
        }

        public int getHeight()
        {
            return base.Height;
        }

        public Cadencii.Gui.Dimension getSize()
        {
            return new Cadencii.Gui.Dimension(base.Size.Width, base.Size.Height);
        }

        public void setSize(int width, int height)
        {
			base.Size =new System.Drawing.Size(width, height);
        }

        public void setSize(Cadencii.Gui.Dimension d)
        {
            setSize(d.Width, d.Height);
        }

        public void setBackground(Cadencii.Gui.Color color)
        {
            base.BackColor = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        }

        public Cadencii.Gui.Color getBackground()
        {
            return new Cadencii.Gui.Color(base.BackColor.R, base.BackColor.G, base.BackColor.B);
        }

        public void setForeground(Cadencii.Gui.Color color)
        {
			base.ForeColor = color.ToNative ();
        }

        public Cadencii.Gui.Color getForeground()
        {
            return new Cadencii.Gui.Color(base.ForeColor.R, base.ForeColor.G, base.ForeColor.B);
        }

        public bool isEnabled()
        {
            return base.Enabled;
        }

        public void setEnabled(bool value)
        {
            base.Enabled = value;
        }

        public void requestFocus()
        {
            base.Focus();
        }

        public bool isFocusOwner()
        {
            return base.Focused;
        }

        public void setPreferredSize(Cadencii.Gui.Dimension size)
        {
			base.Size =new System.Drawing.Size(size.Width, size.Height);
        }

        public Cadencii.Gui.Font getFont()
        {
            return new Cadencii.Gui.Font(base.Font);
        }

        public void setFont(Cadencii.Gui.Font font)
        {
            if (font == null) {
                return;
            }
			if ((System.Drawing.Font) font.NativeFont == null) {
                return;
            }
			base.Font = (System.Drawing.Font) font.NativeFont;
        }
        #endregion

        #region common APIs of org.kbinani.*
        // root implementation is in BForm.cs
        public Cadencii.Gui.Point pointToScreen(Cadencii.Gui.Point point_on_client)
        {
            Cadencii.Gui.Point p = getLocationOnScreen();
            return new Cadencii.Gui.Point(p.X + point_on_client.X, p.Y + point_on_client.Y);
        }

        public Cadencii.Gui.Point pointToClient(Cadencii.Gui.Point point_on_screen)
        {
            Cadencii.Gui.Point p = getLocationOnScreen();
            return new Cadencii.Gui.Point(point_on_screen.X - p.X, point_on_screen.Y - p.Y);
        }

        public Object getTag()
        {
            return base.Tag;
        }

        public void setTag(Object value)
        {
            base.Tag = value;
        }
        #endregion

        private LineGraphDrawer getGraphDrawer()
        {
            if (mGraphDrawer == null) {
                mGraphDrawer = new LineGraphDrawer(LineGraphDrawer.TYPE_STEP);
            }
            return mGraphDrawer;
        }

        /// <summary>
        /// 描画幅が2ピクセルのストロークを取得します
        /// </summary>
        /// <returns></returns>
        private Stroke getStroke2px()
        {
            if (mStroke2px == null) {
                mStroke2px = new Stroke(2.0f);
            }
            return mStroke2px;
        }

        /// <summary>
        /// デフォルトのストロークを取得します
        /// </summary>
        /// <returns></returns>
        private Stroke getStrokeDefault()
        {
            if (mStrokeDefault == null) {
                mStrokeDefault = new Stroke();
            }
            return mStrokeDefault;
        }

        public void applyLanguage()
        {
        }

        public void applyFont(Cadencii.Gui.Font font)
        {
            AwtHost.Current.ApplyFontRecurse(this, font);
            Utility.applyContextMenuFontRecurse(cmenuSinger, font);
            Utility.applyContextMenuFontRecurse(cmenuCurve, font);
        }

        private int getMaxColumns()
        {
            int max_columns = EditorManager.keyWidth / AppConfig.MIN_KEY_WIDTH;
            if (max_columns < 1) {
                max_columns = 1;
            }
            return max_columns;
        }

        public int getRowsPerColumn()
        {
            int max_columns = getMaxColumns();
            int row_per_column = mViewingCurves.Count / max_columns;
            if (row_per_column * max_columns < mViewingCurves.Count) {
                row_per_column++;
            }
            return row_per_column;
        }

        /// <summary>
        /// このコントロールの推奨最小表示高さを取得します
        /// </summary>
        public int getPreferredMinSize()
        {
            return TS.HEIGHT_WITHOUT_CURVE + TS.UNIT_HEIGHT_PER_CURVE * getRowsPerColumn();
        }

        /// <summary>
        /// このコントロールの親ウィンドウを取得します
        /// </summary>
        /// <returns></returns>
        public FormMain getMainForm()
        {
            return mMainWindow;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="command"></param>
        /// <param name="register">Undo/Redo用バッファにExecuteの結果を格納するかどうかを指定するフラグ</param>
        private void executeCommand(CadenciiCommand command, bool register)
        {
            if (register) {
                EditorManager.editHistory.register(MusicManager.getVsqFile().executeCommand(command));
            } else {
                MusicManager.getVsqFile().executeCommand(command);
            }
            try {
                if (CommandExecuted != null) {
                    CommandExecuted.Invoke(this, new EventArgs());
                }
            } catch (Exception ex) {
                Logger.StdErr("TrackSelector#executeCommand; ex=" + ex);
            }
        }

        public ValuePair<int, int> getSelectedRegion()
        {
            int x0 = EditorManager.mCurveSelectedInterval.getStart();
            int x1 = EditorManager.mCurveSelectedInterval.getEnd();
            int min = Math.Min(x0, x1);
            int max = Math.Max(x0, x1);
            return new ValuePair<int, int>(min, max);
        }

        /// <summary>
        /// 現在最前面に表示され，編集可能となっているカーブの種類を取得または設定します
        /// </summary>
        public CurveType getSelectedCurve()
        {
            return mSelectedCurve;
        }

        public void setSelectedCurve(CurveType value)
        {
            CurveType old = mSelectedCurve;
            mSelectedCurve = value;
            if (!old.equals(mSelectedCurve)) {
                mLastSelectedCurve = old;
                try {
                    if (SelectedCurveChanged != null) {
                        SelectedCurveChanged.Invoke(this, mSelectedCurve);
                    }
                } catch (Exception ex) {
                    Logger.StdErr("TrackSelector#setSelectedCurve; ex=" + ex);
                }
            }
        }

        /// <summary>
        /// エディタのy方向の位置から，カーブの値を求めます
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public int valueFromYCoord(int y)
        {
            int max = 127;
            int min = 0;
            if (mSelectedCurve.equals(CurveType.VEL)) {
                int selected = EditorManager.Selected;
                if (EditorManager.mDrawIsUtau[selected - 1]) {
                    max = UstEvent.MAX_INTENSITY;
                    min = UstEvent.MIN_INTENSITY;
                } else {
                    max = mSelectedCurve.getMaximum();
                    min = mSelectedCurve.getMinimum();
                }
            } else {
                max = mSelectedCurve.getMaximum();
                min = mSelectedCurve.getMinimum();
            }
            return valueFromYCoord(y, max, min);
        }

        public int valueFromYCoord(int y, int max, int min)
        {
            int oy = getHeight() - 42;
            float order = getGraphHeight() / (float)(max - min);
            return (int)((oy - y) / order) + min;
        }

        public int yCoordFromValue(int value)
        {
            int max = 127;
            int min = 0;
            if (mSelectedCurve.equals(CurveType.VEL)) {
                int selected = EditorManager.Selected;
                if (EditorManager.mDrawIsUtau[selected - 1]) {
                    max = UstEvent.MAX_INTENSITY;
                    min = UstEvent.MIN_INTENSITY;
                } else {
                    max = mSelectedCurve.getMaximum();
                    min = mSelectedCurve.getMinimum();
                }
            } else {
                max = mSelectedCurve.getMaximum();
                min = mSelectedCurve.getMinimum();
            }
            return yCoordFromValue(value, max, min);
        }

        public int yCoordFromValue(int value, int max, int min)
        {
            int oy = getHeight() - 42;
            float order = getGraphHeight() / (float)(max - min);
            return oy - (int)((value - min) * order);
        }

        /// <summary>
        /// カーブエディタを表示するかどうかを取得します
        /// </summary>
        public bool isCurveVisible()
        {
            return mCurveVisible;
        }

        /// <summary>
        /// カーブエディタを表示するかどうかを設定します
        /// </summary>
        /// <param name="value"></param>
        public void setCurveVisible(bool value)
        {
            mCurveVisible = value;
        }

        private Graphics getGraphics()
        {
            if (mGraphics == null) {
                mGraphics = new Graphics();
            }
            return mGraphics;
        }

        /// <summary>
        /// オーバーライドされます
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            Graphics g = getGraphics();
		g.NativeGraphics = e.Graphics;
            paint(g);
        }

        /// <summary>
        /// x軸方向の表示倍率。pixel/clock
        /// </summary>
        public float getScaleY()
        {
            int max = mSelectedCurve.getMaximum();
            int min = mSelectedCurve.getMinimum();
            int oy = getHeight() - 42;
            return getGraphHeight() / (float)(max - min);
        }

        /// <summary>
        /// 指定したコントロールカーブの名前を表示するボックスが，どの位置にあるかを計算します．
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public Rectangle getRectFromCurveType(CurveType curve)
        {
            int row_per_column = getRowsPerColumn();

            int centre = (getGraphHeight() + TS.UNIT_HEIGHT_PER_CURVE) / 2 + 3;
            int index = 100;
            for (int i = 0; i < mViewingCurves.Count; i++) {
                if (mViewingCurves[i].equals(curve)) {
                    index = i;
                    break;
                }
            }
            int ix = index / row_per_column;
            int iy = index - ix * row_per_column;
            int x = 7 + ix * AppConfig.MIN_KEY_WIDTH;
            int y = centre - row_per_column * TS.UNIT_HEIGHT_PER_CURVE / 2 + 2 + TS.UNIT_HEIGHT_PER_CURVE * iy;
            int min_size = getPreferredMinSize();
            if (mLastPreferredMinHeight != min_size) {
                try {
                    if (PreferredMinHeightChanged != null) {
                        PreferredMinHeightChanged.Invoke(this, new EventArgs());
                    }
                } catch (Exception ex) {
                    Logger.StdErr("TrackSelector#getRectFromCurveType; ex=" + ex);
                }
                mLastPreferredMinHeight = min_size;
            }
            return new Rectangle(x, y, 56, 14);
        }

        /// <summary>
        /// コントロール画面を描画します
        /// </summary>
        /// <param name="graphics"></param>
        public void paint(Graphics graphics)
        {
            int width = getWidth();
            int height = getHeight();
            int graph_height = getGraphHeight();
            Dimension size = new Dimension(width + 2, height);
            Graphics g = (Graphics)graphics;
            Color brs_string = Cadencii.Gui.Colors.Black;
            Color rect_curve = new Color(41, 46, 55);
            int centre = TS.HEADER + graph_height / 2;
			g.setColor(Cadencii.Gui.Colors.DarkGray);
            g.fillRect(0, size.Height - 2 * TS.OFFSET_TRACK_TAB, size.Width, 2 * TS.OFFSET_TRACK_TAB);
            int numeric_view = mMouseValue;
			Point p = pointToClient(Cadencii.Gui.Screen.Instance.GetScreenMousePosition());
            Point mouse = new Point(p.X, p.Y);
            VsqFileEx vsq = MusicManager.getVsqFile();
            int selected = EditorManager.Selected;
            int key_width = EditorManager.keyWidth;
            int stdx = EditorManager.MainWindow.Model.StartToDrawX;
            int graph_max_y = TS.HEADER + graph_height;
            int graph_min_y = TS.HEADER;

            try {
                #region SINGER
                Shape last = g.getClip();
                g.setColor(EditorManager.COLOR_BORDER);
                g.drawLine(0, size.Height - 2 * TS.OFFSET_TRACK_TAB,
                            size.Width - 0, size.Height - 2 * TS.OFFSET_TRACK_TAB);
				g.setFont(EditorConfig.baseFont8);
                g.setColor(brs_string);
                g.drawString(
                    "SINGER",
                    9,
                    size.Height - 2 * TS.OFFSET_TRACK_TAB + TS.OFFSET_TRACK_TAB / 2 - AppConfig.baseFont8OffsetHeight);
                g.clipRect(key_width, size.Height - 2 * TS.OFFSET_TRACK_TAB,
                            size.Width - key_width, TS.OFFSET_TRACK_TAB);
                VsqTrack vsq_track = null;
                if (vsq != null) {
                    vsq_track = vsq.Track[selected];
                }
                if (vsq_track != null) {
                    int ycoord = size.Height - 2 * TS.OFFSET_TRACK_TAB + 1;

                    // 左端での歌手を最初に描画
                    int x_at_left = EditorManager.keyWidth + EditorManager.keyOffset;
                    int clock_at_left = EditorManager.clockFromXCoord(x_at_left);
                    VsqEvent singer_at_left = vsq_track.getSingerEventAt(clock_at_left);
                    if (singer_at_left != null) {
                        Rectangle rc =
                            new Rectangle(x_at_left, ycoord,
                                           TS.SINGER_ITEM_WIDTH, TS.OFFSET_TRACK_TAB - 2);
						g.setColor(Cadencii.Gui.Colors.LightGray);
                        g.fillRect(rc.X, rc.Y, rc.Width, rc.Height);
                        g.setColor(TS.COLOR_SINGERBOX_BORDER);
                        g.drawRect(rc.X, rc.Y, rc.Width, rc.Height);
                        g.setColor(brs_string);
                        g.drawString(
                            singer_at_left.ID.IconHandle.IDS,
                            rc.X, rc.Y + TS.OFFSET_TRACK_TAB / 2 - AppConfig.baseFont8OffsetHeight);
                    }

                    // 歌手設定を順に描画
                    int event_count = vsq_track.getEventCount();
                    for (int i = 0; i < event_count; i++) {
                        VsqEvent ve = vsq_track.getEvent(i);
                        if (ve.ID.type != VsqIDType.Singer) {
                            continue;
                        }
                        int clock = ve.Clock;
                        IconHandle singer_handle = (IconHandle)ve.ID.IconHandle;
                        int x = EditorManager.xCoordFromClocks(clock);
                        if (x < x_at_left) {
                            continue;
                        }
                        Rectangle rc =
                            new Rectangle(x, ycoord,
                                           TS.SINGER_ITEM_WIDTH, TS.OFFSET_TRACK_TAB - 2);
                        if (EditorManager.itemSelection.isEventContains(selected, ve.InternalID)) {
                            g.setColor(EditorManager.getHilightColor());
                        } else {
                            g.setColor(Cadencii.Gui.Colors.White);
                        }
                        g.fillRect(rc.X, rc.Y, rc.Width, rc.Height);
                        g.setColor(TS.COLOR_SINGERBOX_BORDER);
                        g.drawRect(rc.X, rc.Y, rc.Width, rc.Height);
                        g.setColor(brs_string);
                        g.drawString(
                            singer_handle.IDS,
                            rc.X, rc.Y + TS.OFFSET_TRACK_TAB / 2 - AppConfig.baseFont8OffsetHeight);
                    }
                }
                g.setClip(last);
                #endregion

                #region トラック選択欄
                int selecter_width = getSelectorWidth();
                g.setColor(EditorManager.COLOR_BORDER);
                g.drawLine(0, size.Height - TS.OFFSET_TRACK_TAB,
                            size.Width, size.Height - TS.OFFSET_TRACK_TAB);
                g.setColor(brs_string);
                g.drawString("TRACK", 9, size.Height - TS.OFFSET_TRACK_TAB + TS.OFFSET_TRACK_TAB / 2 - AppConfig.baseFont8OffsetHeight);
                if (vsq != null) {
                    for (int i = 0; i < ApplicationGlobal.MAX_NUM_TRACK; i++) {
                        int x = key_width + i * selecter_width;
#if DEBUG
                        try {
#endif
                            drawTrackTab(g,
                                          new Rectangle(x, size.Height - TS.OFFSET_TRACK_TAB + 1, selecter_width, TS.OFFSET_TRACK_TAB - 1),
                                          (i + 1 < vsq.Track.Count) ? (i + 1) + " " + vsq.Track[i + 1].getName() : "",
                                          (i == selected - 1),
                                          vsq_track.getCommon().PlayMode >= 0,
                                          EditorManager.getRenderRequired(i + 1),
                                          EditorManager.HILIGHT[i],
                                          EditorManager.RENDER[i]);
#if DEBUG
                        } catch (Exception ex) {
                            CDebug.WriteLine("TrackSelector.DrawTo; ex=" + ex);
                        }
#endif
                    }
                }
                #endregion

                int clock_at_mouse = EditorManager.clockFromXCoord(mouse.X);
                int pbs_at_mouse = 0;
                if (mCurveVisible) {
                    #region カーブエディタ
                    // カーブエディタの下の線
                    g.setColor(new Color(156, 161, 169));
                    g.drawLine(key_width, size.Height - 42,
                                size.Width - 3, size.Height - 42);

                    // カーブエディタの上の線
                    g.setColor(new Color(46, 47, 50));
                    g.drawLine(key_width, TS.HEADER,
                                size.Width - 3, TS.HEADER);

                    g.setColor(new Color(125, 123, 124));
                    g.drawLine(key_width, 0,
                                key_width, size.Height - 1);

                    if (EditorManager.IsCurveSelectedIntervalEnabled) {
                        int x0 = EditorManager.xCoordFromClocks(EditorManager.mCurveSelectedInterval.getStart());
                        int x1 = EditorManager.xCoordFromClocks(EditorManager.mCurveSelectedInterval.getEnd());
                        g.setColor(TS.COLOR_A072R255G255B255);
                        g.fillRect(x0, TS.HEADER, x1 - x0, graph_height);
                    }

                    #region 小節ごとのライン
                    if (vsq != null) {
                        int dashed_line_step = EditorManager.getPositionQuantizeClock();
                        g.clipRect(key_width, TS.HEADER, size.Width - key_width, size.Height - 2 * TS.OFFSET_TRACK_TAB);
                        Color white100 = new Color(0, 0, 0, 100);
                        for (Iterator<VsqBarLineType> itr = vsq.getBarLineIterator(EditorManager.clockFromXCoord(width)); itr.hasNext(); ) {
                            VsqBarLineType blt = itr.next();
                            int x = EditorManager.xCoordFromClocks(blt.clock());
                            int local_clock_step = 480 * 4 / blt.getLocalDenominator();
                            if (blt.isSeparator()) {
                                g.setColor(white100);
                                g.drawLine(x, size.Height - 42 - 1, x, 8 + 1);
                            } else {
                                g.setColor(white100);
                                g.drawLine(x, centre - 5, x, centre + 6);
                                Color pen = new Color(12, 12, 12);
                                g.setColor(pen);
                                g.drawLine(x, 8, x, 14);
                                g.drawLine(x, size.Height - 43, x, size.Height - 42 - 6);
                            }
                            if (dashed_line_step > 1 && EditorManager.isGridVisible()) {
                                int numDashedLine = local_clock_step / dashed_line_step;
                                Color pen = new Color(65, 65, 65);
                                g.setColor(pen);
                                for (int i = 1; i < numDashedLine; i++) {
                                    int x2 = EditorManager.xCoordFromClocks(blt.clock() + i * dashed_line_step);
                                    g.drawLine(x2, centre - 2, x2, centre + 3);
                                    g.drawLine(x2, 8, x2, 12);
                                    g.drawLine(x2, size.Height - 43, x2, size.Height - 43 - 4);
                                }
                            }
                        }
                        g.setClip(null);
                    }
                    #endregion

                    if (vsq_track != null) {
                        Color color = EditorManager.getHilightColor();
                        Color front = new Color(color.R, color.G, color.B, 150);
                        Color back = new Color(255, 249, 255, 44);
                        Color vel_color = new Color(64, 78, 30);

                        // 後ろに描くカーブ
                        if (mLastSelectedCurve.equals(CurveType.VEL) || mLastSelectedCurve.equals(CurveType.Accent) || mLastSelectedCurve.equals(CurveType.Decay)) {
                            drawVEL(g, vsq_track, back, false, mLastSelectedCurve);
                        } else if (mLastSelectedCurve.equals(CurveType.VibratoRate) || mLastSelectedCurve.equals(CurveType.VibratoDepth)) {
                            drawVibratoControlCurve(g, vsq_track, mLastSelectedCurve, back, false);
                        } else {
                            VsqBPList list_back = vsq_track.getCurve(mLastSelectedCurve.getName());
                            if (list_back != null) {
                                drawVsqBPList(g, list_back, back, false);
                            }
                        }

                        // 手前に描くカーブ
                        if (mSelectedCurve.equals(CurveType.VEL) || mSelectedCurve.equals(CurveType.Accent) || mSelectedCurve.equals(CurveType.Decay)) {
                            drawVEL(g, vsq_track, vel_color, true, mSelectedCurve);
                        } else if (mSelectedCurve.equals(CurveType.VibratoRate) || mSelectedCurve.equals(CurveType.VibratoDepth)) {
                            drawVibratoControlCurve(g, vsq_track, mSelectedCurve, front, true);
                        } else if (mSelectedCurve.equals(CurveType.Env)) {
                            drawEnvelope(g, selected, front);
                        } else {
                            VsqBPList list_front = vsq_track.getCurve(mSelectedCurve.getName());
                            if (list_front != null) {
                                drawVsqBPList(g, list_front, front, true);
                            }
                            if (mSelectedCurve.equals(CurveType.PIT)) {
                                #region PBSの値に応じて，メモリを記入する
								System.Drawing.Drawing2D.SmoothingMode old = ((System.Drawing.Graphics) g.NativeGraphics).SmoothingMode;
								((System.Drawing.Graphics) g.NativeGraphics).SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                Color nrml = new Color(0, 0, 0, 190);
                                Color dash = new Color(0, 0, 0, 128);
                                Stroke nrml_stroke = new Stroke();
                                Stroke dash_stroke = new Stroke(1.0f, 0, 0, 10.0f, new float[] { 2.0f, 2.0f }, 0.0f);
                                VsqBPList pbs = vsq_track.MetaText.PBS;
                                pbs_at_mouse = pbs.getValue(clock_at_mouse);
                                int c = pbs.size();
                                int premeasure = vsq.getPreMeasureClocks();
                                int clock_start = EditorManager.clockFromXCoord(key_width);
                                int clock_end = EditorManager.clockFromXCoord(width);
                                if (clock_start < premeasure && premeasure < clock_end) {
                                    clock_start = premeasure;
                                }
                                int last_pbs = pbs.getValue(clock_start);
                                int last_clock = clock_start;
                                int ycenter = yCoordFromValue(0);
                                g.setColor(nrml);
                                g.drawLine(key_width, ycenter, width, ycenter);
                                for (int i = 0; i < c; i++) {
                                    int cl = pbs.getKeyClock(i);
                                    if (cl < clock_start) {
                                        continue;
                                    }
                                    if (clock_end < cl) {
                                        break;
                                    }
                                    int thispbs = pbs.getElement(i);
                                    if (last_pbs == thispbs) {
                                        continue;
                                    }
                                    // last_clockからclの範囲で，PBSの値がlas_pbs
                                    int max = last_pbs;
                                    int min = -last_pbs;
                                    int x1 = EditorManager.xCoordFromClocks(last_clock);
                                    int x2 = EditorManager.xCoordFromClocks(cl);
                                    for (int j = min + 1; j <= max - 1; j++) {
                                        if (j == 0) {
                                            continue;
                                        }
                                        int y = yCoordFromValue((int)(j * 8192 / (double)last_pbs));
                                        if (j % 2 == 0) {
                                            g.setColor(nrml);
                                            g.setStroke(nrml_stroke);
                                            g.drawLine(x1, y, x2, y);
                                        } else {
                                            g.setColor(dash);
                                            g.setStroke(dash_stroke);
                                            g.drawLine(x1, y, x2, y);
                                        }
                                    }
                                    g.setStroke(new Stroke());
                                    last_clock = cl;
                                    last_pbs = thispbs;
                                }
                                int max0 = last_pbs;
                                int min0 = -last_pbs;
                                int x10 = EditorManager.xCoordFromClocks(last_clock);
                                int x20 = EditorManager.xCoordFromClocks(clock_end);
                                for (int j = min0 + 1; j <= max0 - 1; j++) {
                                    if (j == 0) {
                                        continue;
                                    }
                                    int y = yCoordFromValue((int)(j * 8192 / (double)last_pbs));
                                    Color pen = dash;
                                    if (j % 2 == 0) {
                                        pen = nrml;
                                    }
                                    g.setColor(pen);
                                    g.drawLine(x10, y, x20, y);
                                }
								((System.Drawing.Graphics) g.NativeGraphics).SmoothingMode = old;
                                #endregion
                            }
                            drawAttachedCurve(g, vsq.AttachedCurves.get(EditorManager.Selected - 1).get(mSelectedCurve));
                        }
                    }

                    if (EditorManager.IsWholeSelectedIntervalEnabled) {
                        int start = EditorManager.xCoordFromClocks(EditorManager.mWholeSelectedInterval.getStart()) + 2;
                        int end = EditorManager.xCoordFromClocks(EditorManager.mWholeSelectedInterval.getEnd()) + 2;
                        g.setColor(TS.COLOR_A098R000G000B000);
                        g.fillRect(start, TS.HEADER, end - start, graph_height);
                    }

                    if (mMouseDowned) {
                        #region 選択されたツールに応じて描画
                        int value = valueFromYCoord(mouse.Y);
                        if (clock_at_mouse < vsq.getPreMeasure()) {
                            clock_at_mouse = vsq.getPreMeasure();
                        }
                        int max = mSelectedCurve.getMaximum();
                        int min = mSelectedCurve.getMinimum();
                        if (value < min) {
                            value = min;
                        } else if (max < value) {
                            value = max;
                        }
                        EditTool tool = EditorManager.SelectedTool;
                        if (tool == EditTool.LINE) {
#if OLD_IMPL_MOUSE_TRACER
                            int xini = EditorManager.xCoordFromClocks( m_line_start.x );
                            int yini = yCoordFromValue( m_line_start.y );
                            g.setColor( s_pen_050_140_150 );
                            g.drawLine( xini, yini, EditorManager.xCoordFromClocks( clock_at_mouse ), yCoordFromValue( value ) );
#else
                            if (mMouseTracer.size() > 0) {
                                Point pt = mMouseTracer.iterator().First();
                                int xini = pt.X - stdx;
                                int yini = pt.Y;
                                g.setColor(Cadencii.Gui.Colors.Orange);
                                g.setStroke(getStroke2px());
								((System.Drawing.Graphics) g.NativeGraphics).SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                g.drawLine(xini, yini, EditorManager.xCoordFromClocks(clock_at_mouse), yCoordFromValue(value));
								((System.Drawing.Graphics) g.NativeGraphics).SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
                                g.setStroke(getStrokeDefault());
                            }
#endif
                        } else if (tool == EditTool.PENCIL) {
                            if (mMouseTracer.size() > 0 && !EditorManager.isCurveMode()) {
                                LineGraphDrawer d = getGraphDrawer();
                                d.clear();
                                d.setGraphics(g);
                                d.setBaseLineY(graph_max_y);
                                d.setFill(true);
                                d.setDotMode(LineGraphDrawer.DOTMODE_NO);
                                d.setDrawLine(false);
                                d.setFillColor(TS.COLOR_MOUSE_TRACER);
                                foreach (var pt in mMouseTracer.iterator()) {
                                    int x = pt.X - stdx;
                                    int y = pt.Y;
                                    if (y < graph_min_y) {
                                        y = graph_min_y;
                                    } else if (graph_max_y < y) {
                                        y = graph_max_y;
                                    }
                                    d.append(x, y);
                                }
                                d.flush();

                                /*Vector<Integer> ptx = new Vector<Integer>();
                                Vector<Integer> pty = new Vector<Integer>();
                                int height = getHeight() - 42;

                                int count = 0;
                                int lastx = 0;
                                int lasty = 0;
                                for ( Iterator<Point> itr = mMouseTracer.iterator(); itr.hasNext(); ) {
                                    Point pt = itr.next();
                                    int key = pt.x;
                                    int x = key - stdx;
                                    int y = pt.y;
                                    if ( y < 8 ) {
                                        y = 8;
                                    } else if ( height < y ) {
                                        y = height;
                                    }
                                    if ( count == 0 ) {
                                        lasty = height;
                                    }
                                    ptx.add( x ); pty.add( lasty );
                                    ptx.add( x ); pty.add( y );
                                    lastx = x;
                                    lasty = y;
                                    count++;
                                }

                                ptx.add( lastx ); pty.add( height );
                                g.setColor( new Color( 8, 166, 172, 127 ) );
                                int nPoints = ptx.size();
                                g.fillPolygon( PortUtil.convertIntArray( ptx.toArray( new Integer[] { } ) ),
                                               PortUtil.convertIntArray( pty.toArray( new Integer[] { } ) ),
                                               nPoints );*/
                            }
                        } else if (tool == EditTool.ERASER || tool == EditTool.ARROW) {
                            if (mMouseDownMode == MouseDownMode.CURVE_EDIT && mMouseMoved && EditorManager.mCurveSelectingRectangle.Width != 0) {
                                int xini = EditorManager.xCoordFromClocks(EditorManager.mCurveSelectingRectangle.X);
                                int xend = EditorManager.xCoordFromClocks(EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                int x_start = Math.Min(xini, xend);
                                if (x_start < key_width) {
                                    x_start = key_width;
                                }
                                int x_end = Math.Max(xini, xend);
                                int yini = yCoordFromValue(EditorManager.mCurveSelectingRectangle.Y);
                                int yend = yCoordFromValue(EditorManager.mCurveSelectingRectangle.Y + EditorManager.mCurveSelectingRectangle.Height);
                                int y_start = Math.Min(yini, yend);
                                int y_end = Math.Max(yini, yend);
                                if (y_start < 8) y_start = 8;
                                if (y_end > height - 42 - 8) y_end = height - 42;
                                if (x_start < x_end) {
                                    g.setColor(TS.COLOR_A144R255G255B255);
                                    g.fillRect(x_start, y_start, x_end - x_start, y_end - y_start);
                                }
                            } else if (mMouseDownMode == MouseDownMode.VEL_EDIT && mVelEditSelected.ContainsKey(mVelEditLastSelectedID)) {
                                if (mSelectedCurve.equals(CurveType.VEL)) {
                                    numeric_view = mVelEditSelected[mVelEditLastSelectedID].editing.ID.Dynamics;
                                } else if (mSelectedCurve.equals(CurveType.Accent)) {
                                    numeric_view = mVelEditSelected[mVelEditLastSelectedID].editing.ID.DEMaccent;
                                } else if (mSelectedCurve.equals(CurveType.Decay)) {
                                    numeric_view = mVelEditSelected[mVelEditLastSelectedID].editing.ID.DEMdecGainRate;
                                }
                            }
                        }
                        if (mMouseDownMode == MouseDownMode.SINGER_LIST && EditorManager.SelectedTool != EditTool.ERASER) {
                            foreach (var item in EditorManager.itemSelection.getEventIterator()) {
                                int x = EditorManager.xCoordFromClocks(item.editing.Clock);
                                g.setColor(TS.COLOR_SINGERBOX_BORDER_HILIGHT);
                                g.drawRect(x, size.Height - 2 * TS.OFFSET_TRACK_TAB + 1,
                                           TS.SINGER_ITEM_WIDTH, TS.OFFSET_TRACK_TAB - 2);
                            }
                        }
                        #endregion
                    }
                    #endregion
                }

                if (mCurveVisible) {
                    #region カーブの種類一覧
					Font text_font = EditorConfig.baseFont9;
                    int text_font_height = AppConfig.baseFont9Height;
                    int text_font_offset = AppConfig.baseFont9OffsetHeight;
                    Color font_color_normal = Cadencii.Gui.Colors.Black;
                    g.setColor(new Color(212, 212, 212));
                    g.fillRect(0, 0, key_width, size.Height - 2 * TS.OFFSET_TRACK_TAB);

                    // 現在表示されているカーブの名前
                    g.setFont(text_font);
                    g.setColor(brs_string);
                    bool is_utau_mode = EditorManager.mDrawIsUtau[selected - 1];
                    string name = (is_utau_mode && mSelectedCurve.equals(CurveType.VEL)) ? "INT" : mSelectedCurve.getName();
                    g.drawString(name, 7, text_font_height / 2 - text_font_offset + 1);

                    for (int i = 0; i < mViewingCurves.Count; i++) {
                        CurveType curve = mViewingCurves[i];
                        Rectangle rc = getRectFromCurveType(curve);
                        if (curve.equals(mSelectedCurve) || curve.equals(mLastSelectedCurve)) {
                            g.setColor(new Color(108, 108, 108));
                            g.fillRect(rc.X, rc.Y, rc.Width, rc.Height);
                        }
                        g.setColor(rect_curve);
                        g.drawRect(rc.X, rc.Y, rc.Width, rc.Height);
                        int rc_str_x = rc.X + 2;
                        int rc_str_y = rc.Y + text_font_height / 2 - text_font_offset + 1;
                        string n = curve.getName();
                        if (is_utau_mode && curve.equals(CurveType.VEL)) {
                            n = "INT";
                        }
                        if (curve.equals(mSelectedCurve)) {
                            g.setColor(Cadencii.Gui.Colors.White);
                            g.drawString(n, rc_str_x, rc_str_y);
                        } else {
                            g.setColor(font_color_normal);
                            g.drawString(n, rc_str_x, rc_str_y);
                        }
                    }
                    #endregion
                }

                #region 現在のマーカー
                int marker_x = EditorManager.xCoordFromClocks(EditorManager.getCurrentClock());
                if (key_width <= marker_x && marker_x <= size.Width) {
                    g.setColor(Cadencii.Gui.Colors.White);
                    g.setStroke(new Stroke(2f));
                    g.drawLine(marker_x, 0, marker_x, size.Height - 18);
                    g.setStroke(new Stroke());
                }
                #endregion

                // マウス位置での値
                if (isInRect(mouse.X, mouse.Y, new Rectangle(key_width, TS.HEADER, width, graph_height)) &&
                     mMouseDownMode != MouseDownMode.PRE_UTTERANCE_MOVE &&
                     mMouseDownMode != MouseDownMode.OVERLAP_MOVE &&
                     mMouseDownMode != MouseDownMode.VEL_EDIT) {
                    int align = 1;
                    int valign = 0;
                    int shift = 50;
                    if (mSelectedCurve.equals(CurveType.PIT)) {
                        valign = 1;
                        shift = 100;
                    }
					g.setFont(EditorConfig.baseFont10Bold);
                    g.setColor(Cadencii.Gui.Colors.White);
					g.drawStringEx(
                                           mMouseValue + "",
						EditorConfig.baseFont10Bold,
                                           new Rectangle(mouse.X - 100, mouse.Y - shift, 100, 100),
                                           align,
                                           valign);
                    if (mSelectedCurve.equals(CurveType.PIT)) {
                        float delta_note = mMouseValue * pbs_at_mouse / 8192.0f;
                        align = 1;
                        valign = -1;
						g.drawStringEx(
                                               PortUtil.formatDecimal("#0.00", delta_note),
							EditorConfig.baseFont10Bold,
                                               new Rectangle(mouse.X - 100, mouse.Y, 100, 100),
                                               align,
                                               valign);
                    }
                }
            } catch (Exception ex) {
                Logger.StdErr("TrackSelector#paint; ex= " + ex);
            }
        }

        /// <summary>
        /// 指定したトラックのエンベロープ，先行発音，オーバーラップを画面に描画します
        /// </summary>
        /// <param name="g"></param>
        /// <param name="track"></param>
        /// <param name="fill_color"></param>
        private void drawEnvelope(Graphics g, int track_index, Color fill_color)
        {
            int key_width = EditorManager.keyWidth;
            int width = getWidth();
            int height = getHeight();
            g.setClip(key_width, 0, width - key_width, height);
            int clock_start = EditorManager.clockFromXCoord(key_width);
            int clock_end = EditorManager.clockFromXCoord(width);

            VsqFileEx vsq = MusicManager.getVsqFile();
            VsqTrack track = vsq.Track[track_index];
            VsqEvent itr_prev = null;
            VsqEvent itr_item = null;
            VsqEvent itr_next = null;
			Point mouse = pointToClient(Cadencii.Gui.Screen.Instance.GetScreenMousePosition());

            Color brs = fill_color;
            Point selected_point = new Point();
            bool selected_found = false;
            // yが範囲内なので，xも検索するときtrue
            bool search_mouse = (0 <= mouse.Y && mouse.Y <= height);
            IEnumerator<VsqEvent> itr = track.getNoteEventIterator().GetEnumerator();
            int dotwid = TS.DOT_WID * 2 + 1;
            int tolerance = EditorManager.editorConfig.PxTolerance;
            // 選択アイテムが1個以上あるので，検索するときtrue
            bool search_sel = EditorManager.itemSelection.getEventCount() > 0;
            while (true) {
                bool draw_env_points = false;
                itr_prev = itr_item;
                itr_item = itr_next;
                if (itr.MoveNext()) {
                    itr_next = itr.Current;
                } else {
                    itr_next = null;
                }
                if (itr_item == null && itr_prev == null && itr_next == null) {
                    break;
                }
                if (itr_item == null) {
                    continue;
                }
                VsqEvent item = itr_item;
                if (item.Clock + item.ID.getLength() < clock_start) {
                    continue;
                }
                if (clock_end < item.Clock) {
                    break;
                }
                VsqEvent prev_item = itr_prev;
                VsqEvent next_item = itr_next;
                if (prev_item != null) {
                    if (prev_item.Clock + prev_item.ID.getLength() < item.Clock) {
                        // 直前の音符と接続していないのでnullにする
                        prev_item = null;
                    }
                }
                if (next_item != null) {
                    if (item.Clock + item.ID.getLength() < next_item.Clock) {
                        // 直後の音符と接続していないのでnullにする
                        next_item = null;
                    }
                }
                ByRef<int> preutterance = new ByRef<int>();
                ByRef<int> overlap = new ByRef<int>();
                Polygon points = getEnvelopePoints(vsq.TempoTable, prev_item, item, next_item, preutterance, overlap);
                if (mMouseDownMode == MouseDownMode.ENVELOPE_MOVE && item.InternalID == mEnvelopeEdigintID) {
                    selected_point = new Point(points.xpoints[mEnvelopePointKind], points.ypoints[mEnvelopePointKind]);
                    selected_found = true;
                }

                // 編集中のアイテムだったら描く
                // エンベロープ
                if (!draw_env_points) {
                    if (mMouseDownMode == MouseDownMode.ENVELOPE_MOVE && item.InternalID == mEnvelopeEdigintID) {
                        draw_env_points = true;
                    }
                }
                // 先行発音
                if (!draw_env_points) {
                    if (mMouseDownMode == MouseDownMode.PRE_UTTERANCE_MOVE && item.InternalID == mPreUtteranceEditingID) {
                        draw_env_points = true;
                    }
                }
                // オーバーラップ
                if (!draw_env_points) {
                    if (mMouseDownMode == MouseDownMode.OVERLAP_MOVE && item.InternalID == mOverlapEditingID) {
                        draw_env_points = true;
                    }
                }

                // マウスのx座標が範囲内なら描く
                if (!draw_env_points) {
                    if (search_mouse) {
                        if (points.xpoints[0] - tolerance <= mouse.X && mouse.X <= points.xpoints[points.npoints - 1] + tolerance) {
                            draw_env_points = true;
                        }
                    }
                }

                // 選択されてたら描く
                if (!draw_env_points && search_sel) {
                    if (EditorManager.itemSelection.isEventContains(track_index, item.InternalID)) {
                        draw_env_points = true;
                    }
                }

                // 多角形
                g.setColor(brs);
                g.fillPolygon(points);
                g.setColor(Cadencii.Gui.Colors.White);
                g.drawPolyline(points.xpoints, points.ypoints, points.npoints);

                if (draw_env_points) {
                    // データ点の表示
                    for (int i = 1; i < 6; i++) {
                        Point p = new Point(points.xpoints[i], points.ypoints[i]);
                        Rectangle rc = new Rectangle(p.X - TS.DOT_WID, p.Y - TS.DOT_WID, dotwid, dotwid);
                        g.setColor(TS.COLOR_BEZIER_DOT_NORMAL);
                        g.fillRect(rc.X, rc.Y, rc.Width, rc.Height);
                        g.setColor(TS.COLOR_BEZIER_DOT_NORMAL);
                        g.drawRect(rc.X, rc.Y, rc.Width, rc.Height);
                    }

                    // 旗を描く
                    drawPreutteranceAndOverlap(
                        g,
                        preutterance.value, overlap.value,
                        item.UstEvent.getPreUtterance(), item.UstEvent.getVoiceOverlap());
                }
            }

            // 選択されている点のハイライト表示
            if (selected_found) {
                Rectangle rc = new Rectangle(selected_point.X - TS.DOT_WID, selected_point.Y - TS.DOT_WID, dotwid, dotwid);
                g.setColor(EditorManager.getHilightColor());
                g.fillRect(rc.X, rc.Y, rc.Width, rc.Height);
                g.setColor(TS.COLOR_BEZIER_DOT_NORMAL);
                g.drawRect(rc.X, rc.Y, rc.Width, rc.Height);
            }

            g.setClip(null);
        }

        /// <summary>
        /// 先行発音，またはオーバーラップを表示する旗に描く文字列を取得します
        /// </summary>
        /// <param name="flag_is_pre_utterance">先行発音用の文字列を取得する場合にtrue，そうでなければfalseを指定します</param>
        /// <param name="value">先行発音，またはオーバーラップの値</param>
        /// <returns>旗に描くための文字列（Overlap: 0.00など）</returns>
        private static string getFlagTitle(bool flag_is_pre_utterance, float value)
        {
            if (flag_is_pre_utterance) {
                return "Pre Utterance: " + PortUtil.formatDecimal("0.00", value);
            } else {
                return "Overlap: " + PortUtil.formatDecimal("0.00", value);
            }
        }

        /// <summary>
        /// 指定した文字列を旗に書いたときの，旗のサイズを計算します
        /// </summary>
        /// <param name="flag_title"></param>
        private Dimension getFlagBounds(string flag_title)
        {
            if (mTextWidthPerLetter <= 0.0f) {
				Font font = EditorConfig.baseFont10;
                Dimension s = Utility.measureString(flag_title + " ", font);
                mTextWidthPerLetter = s.Width / (float)flag_title.Length;
                mTextHeight = s.Height;
            }
            return new Dimension((int)(flag_title.Length * mTextWidthPerLetter), mTextHeight);
        }

        /// <summary>
        /// 先行発音とオーバーラップを表示する旗を描画します
        /// </summary>
        /// <param name="g"></param>
        /// <param name="px_preutterance"></param>
        /// <param name="px_overlap"></param>
        /// <param name="preutterance"></param>
        /// <param name="overlap"></param>
        private void drawPreutteranceAndOverlap(Graphics g, int px_preutterance, int px_overlap, float preutterance, float overlap)
        {
            int graph_height = getGraphHeight();
			g.setColor(Cadencii.Gui.Colors.Orange);
            g.drawLine(px_preutterance, TS.HEADER + 1, px_preutterance, graph_height + TS.HEADER);
			g.setColor(Cadencii.Gui.Colors.LawnGreen);
            g.drawLine(px_overlap, TS.HEADER + 1, px_overlap, graph_height + TS.HEADER);

            string s_pre = getFlagTitle(true, preutterance);
            string s_ovl = getFlagTitle(false, overlap);
			Font font = EditorConfig.baseFont10;
            int font_height = AppConfig.baseFont10Height;
            int font_offset = AppConfig.baseFont10OffsetHeight;
            Dimension pre_bounds = getFlagBounds(s_pre);
            Dimension ovl_bounds = getFlagBounds(s_ovl);

            Color pen = new Color(0, 0, 0, 50);
			Color transp = new Color(Cadencii.Gui.Colors.Orange.R, Cadencii.Gui.Colors.Orange.G, Cadencii.Gui.Colors.Orange.B, 50);
            g.setColor(transp);
            g.fillRect(px_preutterance, TS.OFFSET_PRE - TS.FLAG_SPACE, pre_bounds.Width, pre_bounds.Height + TS.FLAG_SPACE * 2);
            g.setColor(pen);
            g.drawRect(px_preutterance, TS.OFFSET_PRE - TS.FLAG_SPACE, pre_bounds.Width, pre_bounds.Height + TS.FLAG_SPACE * 2);
			transp = new Color(Cadencii.Gui.Colors.LawnGreen.R, Cadencii.Gui.Colors.LawnGreen.G, Cadencii.Gui.Colors.LawnGreen.B, 50);
            g.setColor(transp);
            g.fillRect(px_overlap, TS.OFFSET_OVL - TS.FLAG_SPACE, ovl_bounds.Width, ovl_bounds.Height + TS.FLAG_SPACE * 2);
            g.setColor(pen);
            g.drawRect(px_overlap, TS.OFFSET_OVL - TS.FLAG_SPACE, ovl_bounds.Width, ovl_bounds.Height + TS.FLAG_SPACE * 2);

            g.setFont(font);
            g.setColor(Cadencii.Gui.Colors.Black);
            g.drawString(s_pre, px_preutterance + 1, TS.OFFSET_PRE + font_height / 2 - font_offset + 1);
            g.drawString(s_ovl, px_overlap + 1, TS.OFFSET_OVL + font_height / 2 - font_offset + 1);
        }

        /// <summary>
        /// 画面上の指定した点に、コントロールカーブのデータ点があるかどうかを調べます
        /// </summary>
        /// <param name="locx">調べたい点の画面上のx座標</param>
        /// <param name="locy">調べたい点の画面上のy座標</param>
        /// <returns>データ点が見つかれば，データ点のid，そうでなければ-1を返します</returns>
        private long findDataPointAt(int locx, int locy)
        {
            if (mSelectedCurve.equals(CurveType.Accent) ||
                 mSelectedCurve.equals(CurveType.Decay) ||
                 mSelectedCurve.equals(CurveType.Env) ||
                 mSelectedCurve.equals(CurveType.VEL)) {
                return -1;
            }
            if (mSelectedCurve.equals(CurveType.VibratoDepth) ||
                 mSelectedCurve.equals(CurveType.VibratoRate)) {
                //TODO: この辺
            } else {
                VsqBPList list = MusicManager.getVsqFile().Track[EditorManager.Selected].getCurve(mSelectedCurve.getName());
                int count = list.size();
                int w = TS.DOT_WID * 2 + 1;
                for (int i = 0; i < count; i++) {
                    int clock = list.getKeyClock(i);
                    VsqBPPair item = list.getElementB(i);
                    int x = EditorManager.xCoordFromClocks(clock);
                    if (x + TS.DOT_WID < EditorManager.keyWidth) {
                        continue;
                    }
                    if (getWidth() < x - TS.DOT_WID) {
                        break;
                    }
                    int y = yCoordFromValue(item.value);
                    Rectangle rc = new Rectangle(x - TS.DOT_WID, y - TS.DOT_WID, w, w);
                    if (isInRect(locx, locy, rc)) {
                        return item.id;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 画面上の指定した点に、エンベロープのポイントがあるかどうかを調べます
        /// </summary>
        /// <param name="locx">調べたい点の画面上のx座標</param>
        /// <param name="locy">調べたい点の画面上のy座標</param>
        /// <param name="internal_id">見つかったエンベロープ・ポイントを保持しているVsqEventのID</param>
        /// <param name="point_kind">見つかったエンベロープ・ポイントのタイプ。(p1,v1)なら1、(p2,v2)なら2，(p5,v5)なら3，(p3,v3)なら4，(p4,v4)なら5</param>
        /// <returns>見つかった場合は真を、そうでなければ偽を返します</returns>
        private bool findEnvelopePointAt(int locx, int locy, ByRef<int> internal_id, ByRef<int> point_kind)
        {
            return findEnvelopeCore(locx, locy, internal_id, point_kind, null);
        }

        /// <summary>
        /// 画面上の指定した位置に，先行発音またはオーバーラップ用の旗が表示されているかどうかを調べます
        /// </summary>
        /// <param name="locx">調べたい点の画面上のx座標</param>
        /// <param name="locy">調べたい点の画面上のy座標</param>
        /// <param name="internal_id">見つかったイベントを表現するVsqEventのInternalID</param>
        /// <param name="found_flag_was_overlap">見つかった旗がオーバーラップのものであった場合にtrue，それ以外はfalse</param>
        /// <returns>旗が見つかった場合にtrue，それ以外はfalseを返します</returns>
        private bool findPreUtteranceOrOverlapAt(int locx, int locy, ByRef<int> internal_id, ByRef<Boolean> found_flag_was_overlap)
        {
            return findEnvelopeCore(locx, locy, internal_id, null, found_flag_was_overlap);
        }

        /// <summary>
        /// findPreUtteranceOrOverlapAtとfindEnvelopePointAtから呼ばれるユーティリティ
        /// </summary>
        /// <param name="locx"></param>
        /// <param name="locy"></param>
        /// <param name="internal_id"></param>
        /// <param name="point_kind"></param>
        /// <param name="found_flag_was_overlap"></param>
        /// <returns></returns>
        private bool findEnvelopeCore(
            int locx, int locy,
            ByRef<int> internal_id,
            ByRef<int> point_kind, ByRef<Boolean> found_flag_was_overlap)
        {
            internal_id.value = -1;
            if (point_kind != null) {
                point_kind.value = -1;
            }

            int clock_start = EditorManager.clockFromXCoord(EditorManager.keyWidth);
            int clock_end = EditorManager.clockFromXCoord(getWidth());
            int dotwid = TS.DOT_WID * 2 + 1;
            VsqFileEx vsq = MusicManager.getVsqFile();
            IEnumerator<VsqEvent> itr = vsq.Track[EditorManager.Selected].getNoteEventIterator().GetEnumerator();
            VsqEvent itr_prev = null;
            VsqEvent itr_item = null;
            VsqEvent itr_next = null;
            ByRef<int> px_preutterance = new ByRef<int>();
            ByRef<int> px_overlap = new ByRef<int>();
            Dimension size = new Dimension();
            while (true) {
                itr_prev = itr_item;
                itr_item = itr_next;
                if (itr.MoveNext()) {
                    itr_next = itr.Current;
                } else {
                    itr_next = null;
                }
                if (itr_prev == null && itr_item == null && itr_next == null) {
                    break;
                }
                VsqEvent item = itr_item;
                if (item == null) {
                    continue;
                }
                if (item.Clock + item.ID.getLength() < clock_start) {
                    continue;
                }
                if (clock_end < item.Clock) {
                    break;
                }
                VsqEvent prev_item = itr_prev;
                VsqEvent next_item = itr_next;
                if (prev_item != null) {
                    if (prev_item.Clock + prev_item.ID.getLength() < item.Clock) {
                        // 直前の音符と接続していないのでnullにする
                        prev_item = null;
                    }
                }
                if (next_item != null) {
                    if (item.Clock + item.ID.getLength() < next_item.Clock) {
                        // 直後の音符と接続していないのでnullにする
                        next_item = null;
                    }
                }
                // エンベロープの点の座標を計算
                Polygon points = getEnvelopePoints(vsq.TempoTable, prev_item, item, next_item, px_preutterance, px_overlap);
                // エンベロープの点の当たり判定
                if (point_kind != null) {
                    for (int i = 5; i >= 1; i--) {
                        Point p = new Point(points.xpoints[i], points.ypoints[i]);
                        Rectangle rc = new Rectangle(p.X - TS.DOT_WID, p.Y - TS.DOT_WID, dotwid, dotwid);
                        if (isInRect(locx, locy, rc)) {
                            internal_id.value = item.InternalID;
                            point_kind.value = i;
                            return true;
                        }
                    }
                }
                // 先行発音の旗の当たり判定
                if (found_flag_was_overlap != null) {
                    string title_preutterance = getFlagTitle(true, item.UstEvent.getPreUtterance());
                    size = getFlagBounds(title_preutterance);
                    if (Utility.isInRect(locx, locy, px_preutterance.value, TS.OFFSET_PRE - TS.FLAG_SPACE, size.Width, size.Height + TS.FLAG_SPACE * 2)) {
                        internal_id.value = item.InternalID;
                        found_flag_was_overlap.value = false;
                        return true;
                    }
                    // オーバーラップ用の旗の当たり判定
                    string title_overlap = getFlagTitle(false, item.UstEvent.getVoiceOverlap());
                    size = getFlagBounds(title_overlap);
                    if (Utility.isInRect(locx, locy, px_overlap.value, TS.OFFSET_OVL - TS.FLAG_SPACE, size.Width, size.Height + TS.FLAG_SPACE * 2)) {
                        internal_id.value = item.InternalID;
                        found_flag_was_overlap.value = true;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 指定したアイテムのエンベロープを画面に描画するための多角形を取得します
        /// </summary>
        /// <param name="tempo_table">クロックから秒時を計算するためのテンポテーブル</param>
        /// <param name="prev_item">直前にある音符イベント，直前が休符(UTAUでのR)の場合はnull</param>
        /// <param name="item">エンベロープを調べる対象の音符イベント</param>
        /// <param name="next_item">直後にある音符イベント，直後が休符(UTAUでのR)の場合はnull</param>
        /// <param name="px_pre_utteramce">先行発音を描画するための旗のx座標</param>
        /// <param name="px_overlap">オーバーラップを描画するための旗のx座標</param>
        /// <returns>指定した音符イベントのエンベロープを描画するための多角形．x方向の単位は画面上のピクセル単位，y方向の単位はエンベロープの値と同じ単位</returns>
        private Polygon getEnvelopePoints(
            TempoVector tempo_table,
            VsqEvent prev_item, VsqEvent item, VsqEvent next_item,
            ByRef<int> px_pre_utteramce, ByRef<int> px_overlap)
        {
            ByRef<Double> sec_env_start1 = new ByRef<Double>(0.0);
            ByRef<Double> sec_env_end1 = new ByRef<Double>(0.0);
            getEnvelopeRegion(tempo_table, prev_item, item, next_item, sec_env_start1, sec_env_end1);

            UstEvent ust_event1 = item.UstEvent;
            if (ust_event1 == null) {
                ust_event1 = new UstEvent();
            }
            UstEnvelope draw_target = ust_event1.getEnvelope();
            if (draw_target == null) {
                draw_target = new UstEnvelope();
            }
            double sec_pre_utterance1 = ust_event1.getPreUtterance() / 1000.0;
            double sec_overlap1 = ust_event1.getVoiceOverlap() / 1000.0;

            TempoVectorSearchContext context = new TempoVectorSearchContext();
            int px_env_start1 =
                EditorManager.xCoordFromClocks(
                    (int)tempo_table.getClockFromSec(sec_env_start1.value, context));
            if (px_pre_utteramce != null) {
                px_pre_utteramce.value = px_env_start1;
            }
            double sec_p1 = sec_env_start1.value + draw_target.p1 / 1000.0;
            double sec_p2 = sec_env_start1.value + (draw_target.p1 + draw_target.p2) / 1000.0;
            double sec_p5 = sec_env_start1.value + (draw_target.p1 + draw_target.p2 + draw_target.p5) / 1000.0;
            double sec_p3 = sec_env_end1.value - (draw_target.p3 + draw_target.p4) / 1000.0;
            double sec_p4 = sec_env_end1.value - draw_target.p4 / 1000.0;
            int p1 = EditorManager.xCoordFromClocks((int)tempo_table.getClockFromSec(sec_p1, context));
            int p2 = EditorManager.xCoordFromClocks((int)tempo_table.getClockFromSec(sec_p2, context));
            int p5 = EditorManager.xCoordFromClocks((int)tempo_table.getClockFromSec(sec_p5, context));
            int p3 = EditorManager.xCoordFromClocks((int)tempo_table.getClockFromSec(sec_p3, context));
            int p4 = EditorManager.xCoordFromClocks((int)tempo_table.getClockFromSec(sec_p4, context));
            int px_env_end1 = EditorManager.xCoordFromClocks((int)tempo_table.getClockFromSec(sec_env_end1.value, context));
            if (px_overlap != null) {
                px_overlap.value =
                    EditorManager.xCoordFromClocks(
                        (int)tempo_table.getClockFromSec(sec_env_start1.value + sec_overlap1, context));
            }
            int v1 = yCoordFromValue(draw_target.v1);
            int v2 = yCoordFromValue(draw_target.v2);
            int v3 = yCoordFromValue(draw_target.v3);
            int v4 = yCoordFromValue(draw_target.v4);
            int v5 = yCoordFromValue(draw_target.v5);
            int y = yCoordFromValue(0);
            return new Polygon(new int[] { px_env_start1, p1, p2, p5, p3, p4, px_env_end1 },
                                new int[] { y, v1, v2, v5, v3, v4, y },
                                7);
        }

        /// <summary>
        /// 前後の音符の有無や先行発音などにより，音符のエンベロープがどの範囲に及ぶかを調べます
        /// </summary>
        /// <param name="tempo_table">クロックを秒時に変換するためのテンポテーブル</param>
        /// <param name="item_prev">直前の音符．休符であればnullを指定する</param>
        /// <param name="item">調べる対象の音符</param>
        /// <param name="item_next">直後の音符．休符であればnullを指定する</param>
        /// <param name="env_start_sec">エンベロープの開始時刻(秒)</param>
        /// <param name="env_end_sec">エンベロープの終了時刻(秒)</param>
        private void getEnvelopeRegion(
            TempoVector tempo_table,
            VsqEvent item_prev, VsqEvent item, VsqEvent item_next,
            ByRef<Double> env_start_sec, ByRef<Double> env_end_sec)
        {
            double sec_start1 = tempo_table.getSecFromClock(item.Clock);
            double sec_end1 = tempo_table.getSecFromClock(item.Clock + item.ID.getLength());
            UstEvent ust_event1 = item.UstEvent;
            if (ust_event1 == null) {
                ust_event1 = new UstEvent();
            }
            UstEnvelope draw_target = ust_event1.getEnvelope();
            if (draw_target == null) {
                draw_target = new UstEnvelope();
            }
            double sec_pre_utterance1 = ust_event1.getPreUtterance() / 1000.0;
            double sec_overlap1 = ust_event1.getVoiceOverlap() / 1000.0;

            // 先行発音があることによる，この音符のエンベロープの実際の開始位置
            double sec_env_start1 = sec_start1 - sec_pre_utterance1;

            // 直後の音符の有る無しで，この音符のエンベロープの実際の終了位置が変わる
            double sec_env_end1 = sec_end1;
            if (item_next != null && item_next.UstEvent != null) {
                // 直後に音符がある場合
                UstEvent ust_event2 = item_next.UstEvent;
                double sec_pre_utterance2 = ust_event2.getPreUtterance() / 1000.0;
                double sec_overlap2 = ust_event2.getVoiceOverlap() / 1000.0;
                sec_env_end1 = sec_end1 - sec_pre_utterance2 + sec_overlap2;
            }

            env_start_sec.value = sec_env_start1;
            env_end_sec.value = sec_env_end1;
        }

        private void drawTrackTab(Graphics g, Rectangle destRect, string name, bool selected, bool enabled, bool render_required, Color hilight, Color render_button_hilight)
        {
            int x = destRect.X;
            int panel_width = render_required ? destRect.Width - 10 : destRect.Width;
            Color panel_color = enabled ? hilight : new Color(125, 123, 124);
            Color button_color = enabled ? render_button_hilight : new Color(125, 123, 124);
            Color panel_title = Cadencii.Gui.Colors.Black;
            Color button_title = selected ? Cadencii.Gui.Colors.White : Cadencii.Gui.Colors.Black;
            Color border = selected ? Cadencii.Gui.Colors.White : new Color(118, 123, 138);

            // 背景(選択されている場合)
            if (selected) {
                g.setColor(panel_color);
                g.fillRect(destRect.X, destRect.Y, destRect.Width, destRect.Height);
                if (render_required && enabled) {
                    g.setColor(render_button_hilight);
                    g.fillRect(destRect.X + destRect.Width - 10, destRect.Y, 10, destRect.Height);
                }
            }

            // 左縦線
            g.setColor(border);
            g.drawLine(destRect.X, destRect.Y,
                        destRect.X, destRect.Y + destRect.Height - 1);
            if (PortUtil.getStringLength(name) > 0) {
                // 上横線
                g.setColor(border);
                g.drawLine(destRect.X + 1, destRect.Y,
                            destRect.X + destRect.Width, destRect.Y);
            }
            if (render_required) {
                g.setColor(border);
                g.drawLine(destRect.X + destRect.Width - 10, destRect.Y,
                            destRect.X + destRect.Width - 10, destRect.Y + destRect.Height - 1);
            }
            g.clipRect(destRect.X, destRect.Y, destRect.Width, destRect.Height);
			string title = Utility.trimString(name, EditorConfig.baseFont8, panel_width);
			g.setFont(EditorConfig.baseFont8);
            g.setColor(panel_title);
            g.drawString(title, destRect.X + 2, destRect.Y + destRect.Height / 2 - AppConfig.baseFont8OffsetHeight);
            if (render_required) {
                g.setColor(button_title);
                g.drawString("R", destRect.X + destRect.Width - TS.PX_WIDTH_RENDER, destRect.Y + destRect.Height / 2 - AppConfig.baseFont8OffsetHeight);
            }
            if (selected) {
                g.setColor(border);
                g.drawLine(destRect.X + destRect.Width - 1, destRect.Y,
                            destRect.X + destRect.Width - 1, destRect.Y + destRect.Height - 1);
                g.setColor(border);
                g.drawLine(destRect.X, destRect.Y + destRect.Height - 1,
                            destRect.X + destRect.Width, destRect.Y + destRect.Height - 1);
            }
            g.setClip(null);
            g.setColor(EditorManager.COLOR_BORDER);
            g.drawLine(destRect.X + destRect.Width, destRect.Y,
                        destRect.X + destRect.Width, destRect.Y + destRect.Height - 1);
        }

        /// <summary>
        /// トラック選択部分の、トラック1個分の幅を調べます。pixel
        /// </summary>
        public int getSelectorWidth()
        {
            int draft = TS.TRACK_SELECTOR_MAX_WIDTH;
            int maxTotalWidth = getWidth() - EditorManager.keyWidth; // トラックの一覧を表示するのに利用できる最大の描画幅
            int numTrack = 1;
            VsqFileEx vsq = MusicManager.getVsqFile();
            if (vsq != null) {
                numTrack = vsq.Track.Count;
            }
            if (draft * (numTrack - 1) <= maxTotalWidth) {
                return draft;
            } else {
                return (int)((maxTotalWidth) / (numTrack - 1.0f));
            }
        }

        /// <summary>
        /// コンポーネントを再描画する
        /// </summary>
        public void doInvalidate()
        {
            this.Invalidate();
        }

        /// <summary>
        /// ベロシティを、与えられたグラフィックgを用いて描画します
        /// </summary>
        /// <param name="g"></param>
        /// <param name="track"></param>
        /// <param name="color"></param>
        /// <param name="is_front"></param>
        /// <param name="type"></param>
        public void drawVEL(Graphics g, VsqTrack track, Color color, bool is_front, CurveType type)
        {
			Point mouse = pointToClient(Cadencii.Gui.Screen.Instance.GetScreenMousePosition());

            int header = 8;
            int graph_height = getGraphHeight();
            // 描画する値の最大値
            int max = 100;
            // 描画する値の最小値
            int min = 0;

            int height = getHeight();
            int width = getWidth();
            int oy = height - 42;
            Shape last_clip = g.getClip();
            int stdx = EditorManager.MainWindow.Model.StartToDrawX;
            int key_width = EditorManager.keyWidth;
            int xoffset = key_width - stdx;
            g.clipRect(key_width, header, width - key_width, graph_height);
            float scale = EditorManager.MainWindow.Model.ScaleX;
            int selected = EditorManager.Selected;

			g.setFont(EditorConfig.baseFont10Bold);
            bool cursor_should_be_hand = false;
            lock (EditorManager.mDrawObjects) {
                List<DrawObject> target_list = EditorManager.mDrawObjects[selected - 1];
                int count = target_list.Count;
                int i_start = EditorManager.mDrawStartIndex[selected - 1];
                for (int i = i_start; i < count; i++) {
                    DrawObject dobj = target_list[i];
                    if (dobj.mType != DrawObjectType.Note) {
                        continue;
                    }
                    int x = dobj.mRectangleInPixel.X + xoffset;
                    if (x + TS.VEL_BAR_WIDTH < 0) {
                        continue;
                    } else if (width < x) {
                        break;
                    } else {
                        int value = 0;
                        if (type.equals(CurveType.VEL)) {
                            if (EditorManager.mDrawIsUtau[selected - 1]) {
                                value = dobj.mIntensity;
                                max = UstEvent.MAX_INTENSITY;
                                min = UstEvent.MIN_INTENSITY;
                            } else {
                                value = dobj.mVelocity;
                                max = 127;
                                min = 0;
                            }
                        } else if (type.equals(CurveType.Accent)) {
                            value = dobj.mAccent;
                            max = 100;
                            min = 0;
                        } else if (type.equals(CurveType.Decay)) {
                            value = dobj.mDecay;
                            max = 100;
                            min = 0;
                        }
                        //float order = (type.equals( CurveType.VEL )) ? graph_height / 127f : graph_height / 100f;

                        int y = oy - graph_height * (value - min) / (max - min);
                        if (is_front && EditorManager.itemSelection.isEventContains(selected, dobj.mInternalID)) {
                            g.setColor(TS.COLOR_A127R008G166B172);
                            g.fillRect(x, y, TS.VEL_BAR_WIDTH, oy - y);
                            if (mMouseDownMode == MouseDownMode.VEL_EDIT) {
                                int editing = 0;
                                if (mVelEditSelected.ContainsKey(dobj.mInternalID)) {
                                    VsqEvent ve_editing = mVelEditSelected[dobj.mInternalID].editing;
                                    if (mSelectedCurve.equals(CurveType.VEL)) {
                                        if (EditorManager.mDrawIsUtau[selected - 1]) {
                                            editing = ve_editing.UstEvent == null ? 100 : ve_editing.UstEvent.getIntensity();
                                        } else {
                                            editing = ve_editing.ID.Dynamics;
                                        }
                                    } else if (mSelectedCurve.equals(CurveType.Accent)) {
                                        editing = ve_editing.ID.DEMaccent;
                                    } else if (mSelectedCurve.equals(CurveType.Decay)) {
                                        editing = ve_editing.ID.DEMdecGainRate;
                                    }
                                    int edit_y = oy - graph_height * (editing - min) / (max - min);
                                    g.setColor(TS.COLOR_A244R255G023B012);
                                    g.fillRect(x, edit_y, TS.VEL_BAR_WIDTH, oy - edit_y);
                                    g.setColor(Cadencii.Gui.Colors.White);
                                    g.drawString(editing + "", x + TS.VEL_BAR_WIDTH, (edit_y > oy - 20) ? oy - 20 : edit_y);
                                }
                            }
                        } else {
                            g.setColor(color);
                            g.fillRect(x, y, TS.VEL_BAR_WIDTH, oy - y);
                        }
                        if (mMouseDownMode == MouseDownMode.VEL_EDIT) {
                            cursor_should_be_hand = true;
                        } else {
                            if (EditorManager.SelectedTool == EditTool.ARROW && is_front && isInRect(mouse.X, mouse.Y, new Rectangle(x, y, TS.VEL_BAR_WIDTH, oy - y))) {
                                cursor_should_be_hand = true;
                            }
                        }
                    }
                }
            }
            if (cursor_should_be_hand) {
                if (getCursor().getType() != Cadencii.Gui.Cursor.HAND_CURSOR) {
                    setCursor(new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.HAND_CURSOR));
                }
            } else {
                if (getCursor().getType() != Cadencii.Gui.Cursor.DEFAULT_CURSOR) {
                    setCursor(new Cadencii.Gui.Cursor(Cadencii.Gui.Cursor.DEFAULT_CURSOR));
                }
            }
            g.setClip(last_clip);
        }

        /// <summary>
        /// ベジエ曲線によるコントロールカーブを描画します
        /// </summary>
        /// <param name="g"></param>
        /// <param name="chains"></param>
        private void drawAttachedCurve(Graphics g, List<BezierChain> chains)
        {
#if DEBUG
            try {
#endif
                int visibleMinX = EditorManager.keyWidth;
                int visibleMaxX = mMainWindow.pictPianoRoll.Width + EditorManager.keyWidth + EditorManager.keyOffset;
			Color hilight = EditorManager.getHilightColor();
                int chains_count = chains.Count;
                for (int i = 0; i < chains_count; i++) {
                    BezierChain target_chain = chains[i];
                    int chain_id = target_chain.id;
                    if (target_chain.points.Count <= 0) {
                        continue;
                    }
                    BezierPoint next;
                    BezierPoint current = target_chain.points[0];
                    Point pxNext;
                    Point pxCurrent = getScreenCoord(current.getBase());
                    int target_chain_points_count = target_chain.points.Count;
                    bool breaked = false;
                    for (int j = 0; j < target_chain_points_count; j++) {
                        next = target_chain.points[j];
                        int next_x = EditorManager.xCoordFromClocks((int)next.getBase().getX());
                        pxNext = new Point(next_x, yCoordFromValue((int)next.getBase().getY()));
                        Point pxControlCurrent = getScreenCoord(current.getControlRight());
                        Point pxControlNext = getScreenCoord(next.getControlLeft());

                        // ベジエ曲線本体を描く
                        if (isVisibleOnScreen(visibleMinX, visibleMaxX, pxCurrent.X, pxNext.X)) {
                            if (current.getControlRightType() == BezierControlType.None &&
                                 next.getControlLeftType() == BezierControlType.None) {
                                g.setColor(TS.COLOR_BEZIER_CURVE);
                                g.drawLine(pxCurrent.X, pxCurrent.Y, pxNext.X, pxNext.Y);
                            } else {
                                Point ctrl1 = (current.getControlRightType() == BezierControlType.None) ? pxCurrent : pxControlCurrent;
                                Point ctrl2 = (next.getControlLeftType() == BezierControlType.None) ? pxNext : pxControlNext;
                                g.setColor(TS.COLOR_BEZIER_CURVE);
								g.drawBezier(pxCurrent.X, pxCurrent.Y,
                                                        ctrl1.X, ctrl1.Y,
                                                        ctrl2.X, ctrl2.Y,
                                                        pxNext.X, pxNext.Y);
                            }
                        }
                        int minX = pxCurrent.X;
                        int maxX = pxNext.X;

                        if (current.getControlRightType() != BezierControlType.None) {
                            if (isVisibleOnScreen(visibleMinX, visibleMaxX, pxCurrent.X, pxControlCurrent.X)) {
                                g.setColor(TS.COLOR_BEZIER_AUXILIARY);
                                g.drawLine(pxCurrent.X, pxCurrent.Y, pxControlCurrent.X, pxControlCurrent.Y);
                            }
                            minX = Math.Min(minX, pxCurrent.X);
                            maxX = Math.Max(maxX, pxControlCurrent.X);
                        }
                        if (next.getControlLeftType() != BezierControlType.None) {
                            if (isVisibleOnScreen(visibleMinX, visibleMaxX, pxControlNext.X, pxNext.X)) {
                                g.setColor(TS.COLOR_BEZIER_AUXILIARY);
                                g.drawLine(pxNext.X, pxNext.Y, pxControlNext.X, pxControlNext.Y);
                            }
                            minX = Math.Min(minX, pxControlNext.X);
                            maxX = Math.Max(maxX, pxNext.X);
                        }

                        if (visibleMaxX < minX) {
                            breaked = true;
                            break;
                        }

                        // 右コントロール点
                        if (current.getControlRightType() == BezierControlType.Normal) {
                            Rectangle rc = new Rectangle(pxControlCurrent.X - TS.DOT_WID,
                                                          pxControlCurrent.Y - TS.DOT_WID,
                                                          TS.DOT_WID * 2 + 1,
                                                          TS.DOT_WID * 2 + 1);
                            if (chain_id == mEditingChainID && current.getID() == mEditingPointID) {
                                g.setColor(hilight);
                                g.fillOval(rc.X, rc.Y, rc.Width, rc.Height);
                            } else {
                                g.setColor(TS.COLOR_BEZIER_DOT_NORMAL);
                                g.fillOval(rc.X, rc.Y, rc.Width, rc.Height);
                            }
                            g.setColor(TS.COLOR_BEZIER_DOT_NORMAL_DARK);
                            g.drawOval(rc.X, rc.Y, rc.Width, rc.Height);
                        }

                        // 左コントロール点
                        if (next.getControlLeftType() == BezierControlType.Normal) {
                            Rectangle rc = new Rectangle(pxControlNext.X - TS.DOT_WID,
                                                          pxControlNext.Y - TS.DOT_WID,
                                                          TS.DOT_WID * 2 + 1,
                                                          TS.DOT_WID * 2 + 1);
                            if (chain_id == mEditingChainID && next.getID() == mEditingPointID) {
                                g.setColor(hilight);
                                g.fillOval(rc.X, rc.Y, rc.Width, rc.Height);
                            } else {
                                g.setColor(TS.COLOR_BEZIER_DOT_NORMAL);
                                g.fillOval(rc.X, rc.Y, rc.Width, rc.Height);
                            }
                            g.setColor(TS.COLOR_BEZIER_DOT_NORMAL_DARK);
                            g.drawOval(rc.X, rc.Y, rc.Width, rc.Height);
                        }

                        // データ点
                        Rectangle rc2 = new Rectangle(pxCurrent.X - TS.DOT_WID,
                                                        pxCurrent.Y - TS.DOT_WID,
                                                        TS.DOT_WID * 2 + 1,
                                                        TS.DOT_WID * 2 + 1);
                        if (chain_id == mEditingChainID && current.getID() == mEditingPointID) {
                            g.setColor(hilight);
                            g.fillRect(rc2.X, rc2.Y, rc2.Width, rc2.Height);
                        } else {
                            g.setColor(TS.COLOR_BEZIER_DOT_BASE);
                            g.fillRect(rc2.X, rc2.Y, rc2.Width, rc2.Height);
                        }
                        g.setColor(TS.COLOR_BEZIER_DOT_BASE_DARK);
                        g.drawRect(rc2.X, rc2.Y, rc2.Width, rc2.Height);
                        pxCurrent = pxNext;
                        current = next;
                    }
                    if (!breaked) {
                        next = target_chain.points[target_chain.points.Count - 1];
                        pxNext = getScreenCoord(next.getBase());
                        Rectangle rc_last = new Rectangle(pxNext.X - TS.DOT_WID,
                                                           pxNext.Y - TS.DOT_WID,
                                                           TS.DOT_WID * 2 + 1,
                                                           TS.DOT_WID * 2 + 1);
                        if (chain_id == mEditingChainID && next.getID() == mEditingPointID) {
                            g.setColor(hilight);
                            g.fillRect(rc_last.X, rc_last.Y, rc_last.Width, rc_last.Height);
                        } else {
                            g.setColor(TS.COLOR_BEZIER_DOT_BASE);
                            g.fillRect(rc_last.X, rc_last.Y, rc_last.Width, rc_last.Height);
                        }
                        g.setColor(TS.COLOR_BEZIER_DOT_BASE_DARK);
                        g.drawRect(rc_last.X, rc_last.Y, rc_last.Width, rc_last.Height);
                    }
                }
#if DEBUG
            } catch (Exception ex) {
                CDebug.WriteLine("TrackSelector+DrawAttatchedCurve");
                CDebug.WriteLine("    ex=" + ex);
            }
#endif
        }

        /// <summary>
        /// スクリーンの範囲に直線が可視状態となるかを判定する
        /// </summary>
        /// <param name="visibleMinX"></param>
        /// <param name="visibleMaxX"></param>
        /// <param name="startX"></param>
        /// <param name="endX"></param>
        /// <returns></returns>
        private bool isVisibleOnScreen(int visibleMinX, int visibleMaxX, int startX, int endX)
        {
            return ((visibleMinX <= startX && startX <= visibleMaxX) || (visibleMinX <= endX && endX <= visibleMaxX) || (startX < visibleMinX && visibleMaxX <= endX));
        }

        private Point getScreenCoord(PointD pt)
        {
            return new Point(EditorManager.xCoordFromClocks((int)pt.getX()), yCoordFromValue((int)pt.getY()));
        }

        /// <summary>
        /// ビブラートのRate, Depthカーブを描画します
        /// </summary>
        /// <param name="g">描画に使用するグラフィックス</param>
        /// <param name="draw_target">描画対象のトラック</param>
        /// <param name="type">描画するカーブの種類</param>
        /// <param name="color">塗りつぶしに使う色</param>
        /// <param name="is_front">最前面に表示するモードかどうか</param>
        public void drawVibratoControlCurve(Graphics g, VsqTrack draw_target, CurveType type, Color color, bool is_front)
        {
            if (!is_front) {
                return;
            }
            Shape last_clip = g.getClip();
            int graph_height = getGraphHeight();
            int key_width = EditorManager.keyWidth;
            int width = getWidth();
            int height = getHeight();
            g.clipRect(key_width, TS.HEADER,
                        width - key_width, graph_height);

            int cl_start = EditorManager.clockFromXCoord(key_width);
            int cl_end = EditorManager.clockFromXCoord(width);
            int max = type.getMaximum();
            int min = type.getMinimum();

            int oy = height - 42;
            float order = graph_height / (float)(max - min);

            // カーブを描く
            int last_shadow_x = key_width;
            foreach (var ve in draw_target.getNoteEventIterator()) {
                int start = ve.Clock + ve.ID.VibratoDelay;
                int end = ve.Clock + ve.ID.getLength();
                if (end < cl_start) {
                    continue;
                }
                if (cl_end < start) {
                    break;
                }
                if (ve.ID.VibratoHandle == null) {
                    continue;
                }
                VibratoHandle handle = ve.ID.VibratoHandle;
                if (handle == null) {
                    continue;
                }
                int x1 = EditorManager.xCoordFromClocks(start);

                // 左側の影付にする部分を描画
                g.setColor(TS.COLOR_VIBRATO_SHADOW);
                g.fillRect(last_shadow_x, TS.HEADER, x1 - last_shadow_x, graph_height);
                int x2 = EditorManager.xCoordFromClocks(end);
                last_shadow_x = x2;

                if (x1 < x2) {
                    // 描画器を取得、初期化
                    LineGraphDrawer d = getGraphDrawer();
                    d.clear();
                    d.setGraphics(g);
                    d.setBaseLineY(oy);
                    d.setFillColor(color);
                    d.setFill(true);
                    d.setDotMode(LineGraphDrawer.DOTMODE_NO);
                    d.setDrawLine(false);
                    d.setLineColor(Cadencii.Gui.Colors.White);
                    d.setDrawLine(true);

                    int draw_width = x2 - x1;
                    int start_value = 0;

                    // typeに応じてカーブを取得
                    VibratoBPList list = null;
                    if (type.equals(CurveType.VibratoRate)) {
                        start_value = handle.getStartRate();
                        list = handle.getRateBP();
                    } else if (type.equals(CurveType.VibratoDepth)) {
                        start_value = handle.getStartDepth();
                        list = handle.getDepthBP();
                    }
                    if (list == null) {
                        continue;
                    }

                    // 描画
                    int last_y = oy - (int)((start_value - min) * order);
                    d.append(x1, last_y);
                    int c = list.getCount();
                    for (int i = 0; i < c; i++) {
                        VibratoBPPair item = list.getElement(i);
                        int x = x1 + (int)(item.X * draw_width);
                        int y = oy - (int)((item.Y - min) * order);
                        d.append(x, y);
                        last_y = y;
                    }
                    d.append(x2, last_y);

                    d.flush();
                }
            }

            // 右側の影付にする部分を描画
            g.setColor(TS.COLOR_VIBRATO_SHADOW);
            g.fillRect(last_shadow_x, TS.HEADER, width - key_width, graph_height);

            g.setClip(last_clip);
        }

        /// <summary>
        /// BPList(コントロールカーブ)を指定したグラフィックスを用いて描画します
        /// </summary>
        /// <param name="g">描画に使用するグラフィックス</param>
        /// <param name="list">描画するコントロールカーブ</param>
        /// <param name="color">X軸とデータ線の間の塗りつぶしに使用する色</param>
        /// <param name="is_front">最前面に表示するモードかどうか</param>
        public void drawVsqBPList(Graphics g, VsqBPList list, Color color, bool is_front)
        {
			Point pmouse = pointToClient(Cadencii.Gui.Screen.Instance.GetScreenMousePosition());
            int max = list.getMaximum();
            int min = list.getMinimum();
            int graph_height = getGraphHeight();
            int width = getWidth();
            int height = getHeight();
            float order = graph_height / (float)(max - min);
            int oy = height - 42;
            int key_width = EditorManager.keyWidth;

            int start = key_width;
            int start_clock = EditorManager.clockFromXCoord(start);
            int end = width;
            int end_clock = EditorManager.clockFromXCoord(end);

            // グラフ描画器の取得と設定
            LineGraphDrawer d = getGraphDrawer();
            d.clear();
            d.setGraphics(g);
            d.setBaseLineY(oy);
            d.setDotSize(TS.DOT_WID);
            d.setFillColor(color);
            d.setDotColor(Cadencii.Gui.Colors.White);
            d.setLineColor(Cadencii.Gui.Colors.White);
            int dot_mode = is_front ? LineGraphDrawer.DOTMODE_NEAR : LineGraphDrawer.DOTMODE_NO;
            if (pmouse.Y < 0 || height < pmouse.Y) {
                dot_mode = LineGraphDrawer.DOTMODE_NO;
            }
            d.setDotMode(dot_mode);
            d.setDrawLine(is_front);
            d.setMouseX(pmouse.X);

            // グラフの描画
            int first_y = list.getValue(start_clock);
            int last_y = oy - (int)((first_y - min) * order);
            d.append(0, last_y);

            int c = list.size();
            if (c > 0) {
                int first_clock = list.getKeyClock(0);
                int last_x = EditorManager.xCoordFromClocks(first_clock);
                first_y = list.getValue(first_clock);
                last_y = oy - (int)((first_y - min) * order);

                for (int i = 0; i < c; i++) {
                    int clock = list.getKeyClock(i);
                    if (clock < start_clock) {
                        continue;
                    }
                    if (end_clock < clock) {
                        break;
                    }
                    int x = EditorManager.xCoordFromClocks(clock);
                    VsqBPPair v = list.getElementB(i);
                    int y = oy - (int)((v.value - min) * order);
                    d.append(x, y);
                    last_y = y;
                }
            }

            d.append(width + TS.DOT_WID + TS.DOT_WID, last_y);
            d.flush();

            // 最前面のBPListの場合
            if (!is_front) {
                // 最前面じゃなかったら帰る
                return;
            }
            // 選択されているデータ点をハイライト表示する
            int w = TS.DOT_WID * 2 + 1;
            g.setColor(TS.COLOR_DOT_HILIGHT);
            foreach (var id in EditorManager.itemSelection.getPointIDIterator()) {
                VsqBPPairSearchContext ret = list.findElement(id);
                if (ret.index < 0) {
                    continue;
                }
                int clock = ret.clock;
                int value = ret.point.value;

                int x = EditorManager.xCoordFromClocks(clock);
                if (x < key_width) {
                    continue;
                } else if (width < x) {
                    break;
                }
                int y = oy - (int)((value - min) * order);
                g.fillRect(x - TS.DOT_WID, y - TS.DOT_WID, w, w);
            }

            // 移動中のデータ点をハイライト表示する
            if (mMouseDownMode == MouseDownMode.POINT_MOVE) {
                int dx = pmouse.X + EditorManager.MainWindow.Model.StartToDrawX - mMouseDownLocation.X;
                int dy = pmouse.Y - mMouseDownLocation.Y;
                foreach (var item in mMovingPoints) {
                    int x = EditorManager.xCoordFromClocks(item.Clock) + dx;
                    int y = yCoordFromValue(item.Value) + dy;
                    g.setColor(TS.COLOR_DOT_HILIGHT);
                    g.fillRect(x - TS.DOT_WID, y - TS.DOT_WID, w, w);
                }
            }
        }

        /// <summary>
        /// カーブエディタのグラフ部分の高さを取得します(pixel)
        /// </summary>
        public int getGraphHeight()
        {
            return getHeight() - 42 - 8;
        }

        /// <summary>
        /// カーブエディタのグラフ部分の幅を取得します。(pixel)
        /// </summary>
        public int getGraphWidth()
        {
            return getWidth() - EditorManager.keyWidth;
        }

        public void TrackSelector_Load(Object sender, EventArgs e)
        {
            this.SetStyle(System.Windows.Forms.ControlStyles.DoubleBuffer, true);
            this.SetStyle(System.Windows.Forms.ControlStyles.UserPaint, true);
            this.SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint, true);
        }

        public void TrackSelector_MouseClick(Object sender, MouseEventArgs e)
        {
            if (mCurveVisible) {
                if (e.Button == MouseButtons.Left) {
                    // カーブの種類一覧上で発生したイベントかどうかを検査
                    for (int i = 0; i < mViewingCurves.Count; i++) {
                        CurveType curve = mViewingCurves[i];
                        Rectangle r = getRectFromCurveType(curve);
                        if (isInRect(e.X, e.Y, r)) {
                            changeCurve(curve);
                            return;
                        }
                    }
                } else if (e.Button == MouseButtons.Right) {
                    if (0 <= e.X && e.X <= EditorManager.keyWidth &&
                         0 <= e.Y && e.Y <= getHeight() - 2 * TS.OFFSET_TRACK_TAB) {
                        foreach (var tsi in cmenuCurve.Items) {
                            if (tsi is ToolStripMenuItem) {
                                ToolStripMenuItem tsmi = (ToolStripMenuItem)tsi;
                                tsmi.Checked = false;
                                foreach (var tsi2 in tsmi.DropDownItems) {
                                    if (tsi2 is System.Windows.Forms.ToolStripMenuItem) {
                                        System.Windows.Forms.ToolStripMenuItem tsmi2 = (System.Windows.Forms.ToolStripMenuItem)tsi2;
                                        tsmi2.Checked = false;
                                    }
                                }
                            }
                        }
                        RendererKind kind = VsqFileEx.getTrackRendererKind(MusicManager.getVsqFile().Track[EditorManager.Selected]);
                        if (kind == RendererKind.VOCALOID1) {
                            cmenuCurveVelocity.Visible = true;
                            cmenuCurveAccent.Visible = true;
                            cmenuCurveDecay.Visible = true;

                            cmenuCurveSeparator1.Visible = true;
                            cmenuCurveDynamics.Visible = true;
                            cmenuCurveVibratoRate.Visible = true;
                            cmenuCurveVibratoDepth.Visible = true;

                            cmenuCurveSeparator2.Visible = true;
                            cmenuCurveReso1.Visible = true;
                            cmenuCurveReso2.Visible = true;
                            cmenuCurveReso3.Visible = true;
                            cmenuCurveReso4.Visible = true;

                            cmenuCurveSeparator3.Visible = true;
                            cmenuCurveHarmonics.Visible = true;
                            cmenuCurveBreathiness.Visible = true;
                            cmenuCurveBrightness.Visible = true;
                            cmenuCurveClearness.Visible = true;
                            cmenuCurveOpening.Visible = false;
                            cmenuCurveGenderFactor.Visible = true;

                            cmenuCurveSeparator4.Visible = true;
                            cmenuCurvePortamentoTiming.Visible = true;
                            cmenuCurvePitchBend.Visible = true;
                            cmenuCurvePitchBendSensitivity.Visible = true;

                            cmenuCurveSeparator5.Visible = true;
                            cmenuCurveEffect2Depth.Visible = true;
                            cmenuCurveEnvelope.Visible = false;

                            cmenuCurveBreathiness.Text = "Noise";
                            cmenuCurveVelocity.Text = "Velocity";
                        } else if (kind == RendererKind.UTAU || kind == RendererKind.VCNT) {
                            cmenuCurveVelocity.Visible = (kind == RendererKind.UTAU);
                            cmenuCurveAccent.Visible = false;
                            cmenuCurveDecay.Visible = false;

                            cmenuCurveSeparator1.Visible = false;
                            cmenuCurveDynamics.Visible = false;
                            cmenuCurveVibratoRate.Visible = true;
                            cmenuCurveVibratoDepth.Visible = true;

                            cmenuCurveSeparator2.Visible = false;
                            cmenuCurveReso1.Visible = false;
                            cmenuCurveReso2.Visible = false;
                            cmenuCurveReso3.Visible = false;
                            cmenuCurveReso4.Visible = false;

                            cmenuCurveSeparator3.Visible = false;
                            cmenuCurveHarmonics.Visible = false;
                            cmenuCurveBreathiness.Visible = false;
                            cmenuCurveBrightness.Visible = false;
                            cmenuCurveClearness.Visible = false;
                            cmenuCurveOpening.Visible = false;
                            cmenuCurveGenderFactor.Visible = false;

                            cmenuCurveSeparator4.Visible = true;
                            cmenuCurvePortamentoTiming.Visible = false;
                            cmenuCurvePitchBend.Visible = true;
                            cmenuCurvePitchBendSensitivity.Visible = true;

                            cmenuCurveSeparator5.Visible = true;
                            cmenuCurveEffect2Depth.Visible = false;
                            cmenuCurveEnvelope.Visible = true;

                            if (kind == RendererKind.UTAU) {
                                cmenuCurveVelocity.Text = "Intensity";
                            }
                        } else {
                            cmenuCurveVelocity.Visible = true;
                            cmenuCurveAccent.Visible = true;
                            cmenuCurveDecay.Visible = true;

                            cmenuCurveSeparator1.Visible = true;
                            cmenuCurveDynamics.Visible = true;
                            cmenuCurveVibratoRate.Visible = true;
                            cmenuCurveVibratoDepth.Visible = true;

                            cmenuCurveSeparator2.Visible = false;
                            cmenuCurveReso1.Visible = false;
                            cmenuCurveReso2.Visible = false;
                            cmenuCurveReso3.Visible = false;
                            cmenuCurveReso4.Visible = false;

                            cmenuCurveSeparator3.Visible = true;
                            cmenuCurveHarmonics.Visible = false;
                            cmenuCurveBreathiness.Visible = true;
                            cmenuCurveBrightness.Visible = true;
                            cmenuCurveClearness.Visible = true;
                            cmenuCurveOpening.Visible = true;
                            cmenuCurveGenderFactor.Visible = true;

                            cmenuCurveSeparator4.Visible = true;
                            cmenuCurvePortamentoTiming.Visible = true;
                            cmenuCurvePitchBend.Visible = true;
                            cmenuCurvePitchBendSensitivity.Visible = true;

                            cmenuCurveSeparator5.Visible = false;
                            cmenuCurveEffect2Depth.Visible = false;
                            cmenuCurveEnvelope.Visible = false;

                            cmenuCurveBreathiness.Text = "Breathiness";
                            cmenuCurveVelocity.Text = "Velocity";
                        }
                        foreach (var tsi in cmenuCurve.Items) {
                            if (tsi is UiToolStripMenuItem) {
                                var tsmi = (UiToolStripMenuItem)tsi;
                                CurveType ct = getCurveTypeFromMenu(tsmi);
                                if (ct.equals(mSelectedCurve)) {
                                    tsmi.Checked = true;
                                    break;
                                }
                                foreach (var tsi2 in tsmi.DropDownItems) {
                                    if (tsi2 is UiToolStripMenuItem) {
                                        var tsmi2 = (UiToolStripMenuItem)tsi2;
                                        CurveType ct2 = getCurveTypeFromMenu(tsmi2);
                                        if (ct2.equals(mSelectedCurve)) {
                                            tsmi2.Checked = true;
                                            if (ct2.equals(CurveType.Reso1Amp) || ct2.equals(CurveType.Reso1Bw) || ct2.equals(CurveType.Reso1Freq) ||
                                                 ct2.equals(CurveType.Reso2Amp) || ct2.equals(CurveType.Reso2Bw) || ct2.equals(CurveType.Reso2Freq) ||
                                                 ct2.equals(CurveType.Reso3Amp) || ct2.equals(CurveType.Reso3Bw) || ct2.equals(CurveType.Reso3Freq) ||
                                                 ct2.equals(CurveType.Reso4Amp) || ct2.equals(CurveType.Reso4Bw) || ct2.equals(CurveType.Reso4Freq)) {
                                                tsmi.Checked = true;//親アイテムもチェック。Resonance*用
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        cmenuCurve.Show(this, e.X, e.Y);
                    }
                }
            }
        }

        public void SelectNextCurve()
        {
            int index = 0;
            if (mViewingCurves.Count >= 2) {
                for (int i = 0; i < mViewingCurves.Count; i++) {
                    if (mViewingCurves[i].equals(mSelectedCurve)) {
                        index = i;
                        break;
                    }
                }
                index++;
                if (mViewingCurves.Count <= index) {
                    index = 0;
                }
                changeCurve(mViewingCurves[index]);
            }
        }

        public void SelectPreviousCurve()
        {
            int index = 0;
            if (mViewingCurves.Count >= 2) {
                for (int i = 0; i < mViewingCurves.Count; i++) {
                    if (mViewingCurves[i].equals(mSelectedCurve)) {
                        index = i;
                        break;
                    }
                }
                index--;
                if (index < 0) {
                    index = mViewingCurves.Count - 1;
                }
                changeCurve(mViewingCurves[index]);
            }
        }

        public BezierPoint HandleMouseMoveForBezierMove(int clock, int value, int value_raw, BezierPickedSide picked)
        {
            BezierChain target = MusicManager.getVsqFile().AttachedCurves.get(EditorManager.Selected - 1).getBezierChain(mSelectedCurve, EditorManager.itemSelection.getLastBezier().chainID);
            int point_id = EditorManager.itemSelection.getLastBezier().pointID;
            int index = -1;
            for (int i = 0; i < target.points.Count; i++) {
                if (target.points[i].getID() == point_id) {
                    index = i;
                    break;
                }
            }
            float scale_x = EditorManager.MainWindow.Model.ScaleX;
            float scale_y = getScaleY();
            BezierPoint ret = new BezierPoint(0, 0);
            if (index >= 0) {
                BezierPoint item = target.points[index];
                if (picked == BezierPickedSide.BASE) {
                    // データ点を動かす
                    Point old = target.points[index].getBase().toPoint();
                    item.setBase(new PointD(clock, value));
                    if (!BezierChain.isBezierImplicit(target)) {
                        // X軸について陰でなくなった場合
                        // データ点のX座標だけ元に戻し，もう一度チェックを試みる
                        item.setBase(new PointD(old.X, value));
                        if (!BezierChain.isBezierImplicit(target)) {
                            // 駄目ならX, Y両方元に戻す
                            item.setBase(new PointD(old.X, old.Y));
                        }
                    }
                    ret = (BezierPoint)target.points[index].clone();
                } else if (picked == BezierPickedSide.LEFT) {
                    // 左制御点を動かす
                    if (item.getControlLeftType() == BezierControlType.Master) {
                        // 右制御点を同時に動かさない場合
                        PointD old_left = new PointD(item.getControlLeft());
                        item.setControlLeft(new PointD(clock, value_raw));
                        if (!BezierChain.isBezierImplicit(target)) {
                            // X軸について陰でなくなった場合
                            // X座標だけ元に戻し，もう一度チェックを試みる
                            item.setControlLeft(new PointD(old_left.getX(), value_raw));
                            if (!BezierChain.isBezierImplicit(target)) {
                                // 駄目ならX, Y両方戻す
                                item.setControlLeft(old_left);
                            }
                        }
                    } else {
                        // 右制御点を同時に動かす場合(デフォルト)
                        PointD old_left = new PointD(item.getControlLeft());
                        PointD old_right = new PointD(item.getControlRight());
                        PointD old_base = new PointD(item.getBase());

                        // 新しい座標値を計算，設定
                        PointD new_left = new PointD(clock, value_raw);
                        PointD new_right = getCounterPoint(old_base, old_right, new_left, scale_x, scale_y);
                        item.setControlLeft(new_left);
                        item.setControlRight(new_right);

                        // X軸方向に陰かどうかチェック
                        if (!BezierChain.isBezierImplicit(target)) {
                            // 駄目なら，Xを元に戻す
                            new_left.setX(old_left.getX());
                            new_right = getCounterPoint(old_base, old_right, new_left, scale_x, scale_y);
                            item.setControlLeft(new_left);
                            item.setControlRight(new_right);
                            if (!BezierChain.isBezierImplicit(target)) {
                                // それでもだめなら両方戻す
                                item.setControlLeft(old_left);
                                item.setControlRight(old_right);
                            }
                        }
                    }
                    ret = (BezierPoint)item.clone();
                } else if (picked == BezierPickedSide.RIGHT) {
                    // 右制御点を動かす
                    if (item.getControlRightType() == BezierControlType.Master) {
                        // 左制御点を同時に動かさない場合
                        PointD old_right = item.getControlRight();
                        item.setControlRight(new PointD(clock, value));
                        if (!BezierChain.isBezierImplicit(target)) {
                            // Xだけ元に戻す
                            item.setControlRight(new PointD(old_right.getX(), value));
                            if (!BezierChain.isBezierImplicit(target)) {
                                item.setControlRight(old_right);
                            }
                        }
                    } else {
                        // 左制御点を同時に動かす場合(デフォルト)
                        PointD old_left = new PointD(item.getControlLeft());
                        PointD old_right = new PointD(item.getControlRight());
                        PointD old_base = new PointD(item.getBase());

                        // 新しい座標値を計算，設定
                        PointD new_right = new PointD(clock, value_raw);
                        PointD new_left = getCounterPoint(old_base, old_left, new_right, scale_x, scale_y);
                        item.setControlRight(new_right);
                        item.setControlLeft(new_left);

                        // X軸方向に陰かどうかチェック
                        if (!BezierChain.isBezierImplicit(target)) {
                            // 駄目ならXだけ元に戻す
                            new_right.setX(old_right.getX());
                            new_left = getCounterPoint(old_base, old_left, new_right, scale_x, scale_y);
                            item.setControlRight(new_right);
                            item.setControlLeft(new_left);
                            if (!BezierChain.isBezierImplicit(target)) {
                                item.setControlLeft(old_left);
                                item.setControlRight(old_right);
                            }
                        }
                    }
                    ret = (BezierPoint)item.clone();
                }
            }
            return ret;
        }

        /// <summary>
        /// slave_point_original, base_point, moving_pointがこの順で1直線に並んでいる時，
        /// base_pointを回転軸としてmoving_pointを動かした場合に，
        /// 回転に伴ってslave_point_originalが移動した先の座標を計算します．
        /// ただし，上記の各点の座標値はscalex，scaleyを乗じた上で計算されます
        /// </summary>
        /// <param name="base_point"></param>
        /// <param name="slave_point_original"></param>
        /// <param name="moving_point"></param>
        /// <param name="scalex"></param>
        /// <param name="scaley"></param>
        /// <returns></returns>
        private static PointD getCounterPoint(
            PointD base_point,
            PointD slave_point_original,
            PointD moving_point,
            double scalex, double scaley)
        {
            // 移動後の点と回転軸との為す角を計算
            double theta =
                Math.Atan2(
                    (moving_point.getY() - base_point.getY()) * scaley,
                    (moving_point.getX() - base_point.getX()) * scalex);
            // 直線なので，逆サイドの偏角は+180度
            theta += Math.PI;
            // 逆サイドの点と回転軸との距離を計算
            double dx = (slave_point_original.getX() - base_point.getX()) * scalex;
            double dy = (slave_point_original.getY() - base_point.getY()) * scaley;
            double length = Math.Sqrt(dx * dx + dy * dy);
            // 逆サイドの点の座標を計算
            return new PointD(
                length * Math.Cos(theta) / scalex + base_point.getX(),
                length * Math.Sin(theta) / scaley + base_point.getY());
        }

        public BezierPoint HandleMouseMoveForBezierMove(MouseEventArgs e, BezierPickedSide picked)
        {
            int clock = EditorManager.clockFromXCoord(e.X);
            int value = valueFromYCoord(e.Y);
            int value_raw = value;

            if (clock < MusicManager.getVsqFile().getPreMeasure()) {
                clock = MusicManager.getVsqFile().getPreMeasure();
            }
            int max = mSelectedCurve.getMaximum();
            int min = mSelectedCurve.getMinimum();
            if (value < min) {
                value = min;
            } else if (max < value) {
                value = max;
            }
            return HandleMouseMoveForBezierMove(clock, value, value_raw, picked);
        }

        public void TrackSelector_MouseMove(Object sender, MouseEventArgs e)
        {
            int value = valueFromYCoord(e.Y);
            int value_raw = value;
            int max = mSelectedCurve.getMaximum();
            int min = mSelectedCurve.getMinimum();
            int selected = EditorManager.Selected;
            bool is_utau_mode = EditorManager.mDrawIsUtau[selected - 1];
            if (is_utau_mode && mSelectedCurve.equals(CurveType.VEL)) {
                max = UstEvent.MAX_INTENSITY;
                min = UstEvent.MIN_INTENSITY;
            }
            if (value < min) {
                value = min;
            } else if (max < value) {
                value = max;
            }
            mMouseValue = value;

            if (e.Button == MouseButtons.None) {
                return;
            }
            int stdx = EditorManager.MainWindow.Model.StartToDrawX;
            if ((e.X + stdx != mMouseDownLocation.X || e.Y != mMouseDownLocation.Y)) {
                if (mMouseHoverThread != null && mMouseHoverThread.IsAlive) {
                    mMouseHoverThread.Abort();
                }
                if (mMouseDownMode == MouseDownMode.VEL_WAIT_HOVER) {
                    mMouseDownMode = MouseDownMode.VEL_EDIT;
                }
                mMouseMoved = true;
            }
            if (EditorManager.isPlaying()) {
                return;
            }
            int clock = EditorManager.clockFromXCoord(e.X);

            VsqFileEx vsq = MusicManager.getVsqFile();
            if (clock < vsq.getPreMeasure()) {
                clock = vsq.getPreMeasure();
            }

            if (e.Button == MouseButtons.Left &&
                 0 <= e.Y && e.Y <= getHeight() - 2 * TS.OFFSET_TRACK_TAB &&
                 mMouseDownMode == MouseDownMode.CURVE_EDIT) {
                EditTool selected_tool = EditorManager.SelectedTool;
                if (selected_tool == EditTool.PENCIL) {
                    mPencilMoved = e.X + stdx != mMouseDownLocation.X ||
                                     e.Y != mMouseDownLocation.Y;
                    mMouseTracer.append(e.X + stdx, e.Y);
                } else if (selected_tool == EditTool.LINE) {
                    mPencilMoved = e.X + stdx != mMouseDownLocation.X ||
                                     e.Y != mMouseDownLocation.Y;
                } else if (selected_tool == EditTool.ARROW ||
                            selected_tool == EditTool.ERASER) {
                    int draft_clock = clock;
                    if (EditorManager.editorConfig.CurveSelectingQuantized) {
                        int unit = EditorManager.getPositionQuantizeClock();
                        int odd = clock % unit;
                        int nclock = clock;
                        nclock -= odd;
                        if (odd > unit / 2) {
                            nclock += unit;
                        }
                        draft_clock = nclock;
                    }
                    EditorManager.mCurveSelectingRectangle.Width = draft_clock - EditorManager.mCurveSelectingRectangle.X;
                    EditorManager.mCurveSelectingRectangle.Height = value - EditorManager.mCurveSelectingRectangle.Y;
                }
            } else if (mMouseDownMode == MouseDownMode.SINGER_LIST) {
                int dclock = clock - mSingerMoveStartedClock;
                foreach (var item in EditorManager.itemSelection.getEventIterator()) {
                    item.editing.Clock = item.original.Clock + dclock;
                }
            } else if (mMouseDownMode == MouseDownMode.VEL_EDIT) {
                int t_value = valueFromYCoord(e.Y - mVelEditShiftY);
                int d_vel = 0;
                VsqEvent ve_original = mVelEditSelected[mVelEditLastSelectedID].original;
                if (mSelectedCurve.equals(CurveType.VEL)) {
                    if (is_utau_mode) {
                        d_vel = t_value - ((ve_original.UstEvent == null) ? 100 : ve_original.UstEvent.getIntensity());
                    } else {
                        d_vel = t_value - ve_original.ID.Dynamics;
                    }
                } else if (mSelectedCurve.equals(CurveType.Accent)) {
                    d_vel = t_value - mVelEditSelected[mVelEditLastSelectedID].original.ID.DEMaccent;
                } else if (mSelectedCurve.equals(CurveType.Decay)) {
                    d_vel = t_value - mVelEditSelected[mVelEditLastSelectedID].original.ID.DEMdecGainRate;
                }
                foreach (var id in mVelEditSelected.Keys) {
                    if (mSelectedCurve.equals(CurveType.VEL)) {
                        VsqEvent item = mVelEditSelected[id].original;
                        int new_vel = item.ID.Dynamics + d_vel;
                        if (is_utau_mode) {
                            new_vel = item.UstEvent == null ? 100 + d_vel : item.UstEvent.getIntensity() + d_vel;
                        }
                        if (new_vel < min) {
                            new_vel = min;
                        } else if (max < new_vel) {
                            new_vel = max;
                        }
                        if (is_utau_mode) {
                            VsqEvent item_o = mVelEditSelected[id].editing;
                            if (item_o.UstEvent == null) {
                                item_o.UstEvent = new UstEvent();
                            }
                            item_o.UstEvent.setIntensity(new_vel);
                        } else {
                            mVelEditSelected[id].editing.ID.Dynamics = new_vel;
                        }
                    } else if (mSelectedCurve.equals(CurveType.Accent)) {
                        int new_vel = mVelEditSelected[id].original.ID.DEMaccent + d_vel;
                        if (new_vel < min) {
                            new_vel = min;
                        } else if (max < new_vel) {
                            new_vel = max;
                        }
                        mVelEditSelected[id].editing.ID.DEMaccent = new_vel;
                    } else if (mSelectedCurve.equals(CurveType.Decay)) {
                        int new_vel = mVelEditSelected[id].original.ID.DEMdecGainRate + d_vel;
                        if (new_vel < min) {
                            new_vel = min;
                        } else if (max < new_vel) {
                            new_vel = max;
                        }
                        mVelEditSelected[id].editing.ID.DEMdecGainRate = new_vel;
                    }
                }
            } else if (mMouseDownMode == MouseDownMode.BEZIER_MODE) {
                HandleMouseMoveForBezierMove(clock, value, value_raw, EditorManager.itemSelection.getLastBezier().picked);
            } else if (mMouseDownMode == MouseDownMode.BEZIER_ADD_NEW || mMouseDownMode == MouseDownMode.BEZIER_EDIT) {
                BezierChain target = vsq.AttachedCurves.get(selected - 1).getBezierChain(mSelectedCurve, EditorManager.itemSelection.getLastBezier().chainID);
                int point_id = EditorManager.itemSelection.getLastBezier().pointID;
                int index = -1;
                for (int i = 0; i < target.points.Count; i++) {
                    if (target.points[i].getID() == point_id) {
                        index = i;
                        break;
                    }
                }
                if (index >= 0) {
                    BezierPoint item = target.points[index];
                    Point old_right = item.getControlRight().toPoint();
                    Point old_left = item.getControlLeft().toPoint();
                    BezierControlType old_right_type = item.getControlRightType();
                    BezierControlType old_left_type = item.getControlLeftType();
                    int cl = clock;
                    int va = value_raw;
                    int dx = (int)item.getBase().getX() - cl;
                    int dy = (int)item.getBase().getY() - va;
                    if (item.getBase().getX() + dx >= 0) {
                        item.setControlRight(new PointD(clock, value_raw));
                        item.setControlLeft(new PointD(clock + 2 * dx, value_raw + 2 * dy));
                        item.setControlRightType(BezierControlType.Normal);
                        item.setControlLeftType(BezierControlType.Normal);
                        if (!BezierChain.isBezierImplicit(target)) {
                            item.setControlLeft(new PointD(old_left.X, old_left.Y));
                            item.setControlRight(new PointD(old_right.X, old_right.Y));
                            item.setControlLeftType(old_left_type);
                            item.setControlRightType(old_right_type);
                        }
                    }
                }
            } else if (mMouseDownMode == MouseDownMode.ENVELOPE_MOVE) {
                double sec = vsq.getSecFromClock(EditorManager.clockFromXCoord(e.X));
                int v = valueFromYCoord(e.Y);
                if (v < 0) {
                    v = 0;
                } else if (200 < v) {
                    v = 200;
                }
                if (sec < mEnvelopeDotBegin) {
                    sec = mEnvelopeDotBegin;
                } else if (mEnvelopeDotEnd < sec) {
                    sec = mEnvelopeDotEnd;
                }
                if (mEnvelopePointKind == 1) {
                    mEnvelopeEditing.p1 = (int)((sec - mEnvelopeRangeBegin) * 1000.0);
                    mEnvelopeEditing.v1 = v;
                } else if (mEnvelopePointKind == 2) {
                    mEnvelopeEditing.p2 = (int)((sec - mEnvelopeRangeBegin) * 1000.0) - mEnvelopeEditing.p1;
                    mEnvelopeEditing.v2 = v;
                } else if (mEnvelopePointKind == 3) {
                    mEnvelopeEditing.p5 = (int)((sec - mEnvelopeRangeBegin) * 1000.0) - mEnvelopeEditing.p1 - mEnvelopeEditing.p2;
                    mEnvelopeEditing.v5 = v;
                } else if (mEnvelopePointKind == 4) {
                    mEnvelopeEditing.p3 = (int)((mEnvelopeRangeEnd - sec) * 1000.0) - mEnvelopeEditing.p4;
                    mEnvelopeEditing.v3 = v;
                } else if (mEnvelopePointKind == 5) {
                    mEnvelopeEditing.p4 = (int)((mEnvelopeRangeEnd - sec) * 1000.0);
                    mEnvelopeEditing.v4 = v;
                }
            } else if (mMouseDownMode == MouseDownMode.PRE_UTTERANCE_MOVE) {
                int clock_at_downed = EditorManager.clockFromXCoord(mMouseDownLocation.X - stdx);
                double dsec = vsq.getSecFromClock(clock) - vsq.getSecFromClock(clock_at_downed);
                float draft_preutterance = mPreOverlapOriginal.UstEvent.getPreUtterance() - (float)(dsec * 1000);
                mPreOverlapEditing.UstEvent.setPreUtterance(draft_preutterance);
            } else if (mMouseDownMode == MouseDownMode.OVERLAP_MOVE) {
                int clock_at_downed = EditorManager.clockFromXCoord(mMouseDownLocation.X - stdx);
                double dsec = vsq.getSecFromClock(clock) - vsq.getSecFromClock(clock_at_downed);
                float draft_overlap = mPreOverlapOriginal.UstEvent.getVoiceOverlap() + (float)(dsec * 1000);
                mPreOverlapEditing.UstEvent.setVoiceOverlap(draft_overlap);
            }
        }

        /// <summary>
        /// 指定した位置にあるBezierPointを検索します。
        /// </summary>
        /// <param name="location"></param>
        /// <param name="list"></param>
        /// <param name="found_chain"></param>
        /// <param name="found_point"></param>
        /// <param name="found_side"></param>
        /// <param name="dot_width"></param>
        /// <param name="px_tolerance"></param>
        private void findBezierPointAt(int locx,
                                        int locy,
                                        List<BezierChain> list,
                                        ByRef<BezierChain> found_chain,
                                        ByRef<BezierPoint> found_point,
                                        ByRef<BezierPickedSide> found_side,
                                        int dot_width,
                                        int px_tolerance)
        {
            found_chain.value = null;
            found_point.value = null;
            found_side.value = BezierPickedSide.BASE;
            int shift = dot_width + px_tolerance;
            int width = (dot_width + px_tolerance) * 2;
            int c = list.Count;
            Point location = new Point(locx, locy);
            for (int i = 0; i < c; i++) {
                BezierChain bc = list[i];
                foreach (var bp in bc.points) {
                    Point p = getScreenCoord(bp.getBase());
                    Rectangle r = new Rectangle(p.X - shift, p.Y - shift, width, width);
                    if (isInRect(locx, locy, r)) {
                        found_chain.value = bc;
                        found_point.value = bp;
                        found_side.value = BezierPickedSide.BASE;
                        return;
                    }

                    if (bp.getControlLeftType() != BezierControlType.None) {
                        p = getScreenCoord(bp.getControlLeft());
                        r = new Rectangle(p.X - shift, p.Y - shift, width, width);
                        if (isInRect(locx, locy, r)) {
                            found_chain.value = bc;
                            found_point.value = bp;
                            found_side.value = BezierPickedSide.LEFT;
                            return;
                        }
                    }

                    if (bp.getControlRightType() != BezierControlType.None) {
                        p = getScreenCoord(bp.getControlRight());
                        r = new Rectangle(p.X - shift, p.Y - shift, width, width);
                        if (isInRect(locx, locy, r)) {
                            found_chain.value = bc;
                            found_point.value = bp;
                            found_side.value = BezierPickedSide.RIGHT;
                            return;
                        }
                    }
                }
            }
        }

        private void processMouseDownSelectRegion(MouseEventArgs e)
        {
            if (((Keys) Control.ModifierKeys & Keys.Control) != Keys.Control) {
                EditorManager.itemSelection.clearPoint();
            }

            int clock = EditorManager.clockFromXCoord(e.X);
            int quantized_clock = clock;
            int unit = EditorManager.getPositionQuantizeClock();
            int odd = clock % unit;
            quantized_clock -= odd;
            if (odd > unit / 2) {
                quantized_clock += unit;
            }

            int max = mSelectedCurve.getMaximum();
            int min = mSelectedCurve.getMinimum();
            int value = valueFromYCoord(e.Y);
            if (value < min) {
                value = min;
            } else if (max < value) {
                value = max;
            }

			if (EditorManager.editorConfig.CurveSelectingQuantized) {
                EditorManager.mCurveSelectingRectangle = new Rectangle(quantized_clock, value, 0, 0);
            } else {
                EditorManager.mCurveSelectingRectangle = new Rectangle(clock, value, 0, 0);
            }
        }

        public void onMouseDown(Object sender, MouseEventArgs e)
        {
#if DEBUG
            CDebug.WriteLine("TrackSelector_MouseDown");
#endif
            VsqFileEx vsq = MusicManager.getVsqFile();
            mMouseDownLocation.X = e.X + EditorManager.MainWindow.Model.StartToDrawX;
            mMouseDownLocation.Y = e.Y;
            int clock = EditorManager.clockFromXCoord(e.X);
            int selected = EditorManager.Selected;
            int height = getHeight();
            int width = getWidth();
            int key_width = EditorManager.keyWidth;
            VsqTrack vsq_track = vsq.Track[selected];
            mMouseMoved = false;
            mMouseDowned = true;
            if (EditorManager.keyWidth < e.X && clock < vsq.getPreMeasure()) {
                System.Media.SystemSounds.Asterisk.Play();
                return;
            }
            int stdx = EditorManager.MainWindow.Model.StartToDrawX;
            mModifierOnMouseDown = (Keys) Control.ModifierKeys;
            int max = mSelectedCurve.getMaximum();
            int min = mSelectedCurve.getMinimum();
            int value = valueFromYCoord(e.Y);
            if (value < min) {
                value = min;
            } else if (max < value) {
                value = max;
            }

            if (height - TS.OFFSET_TRACK_TAB <= e.Y && e.Y < height) {
                if (e.Button == MouseButtons.Left) {
                    #region MouseDown occured on track list
                    mMouseDownMode = MouseDownMode.TRACK_LIST;
                    //EditorManager.IsCurveSelectedIntervalEnabled = false;
                    mMouseTracer.clear();
                    int selecter_width = getSelectorWidth();
                    if (vsq != null) {
                        for (int i = 0; i < ApplicationGlobal.MAX_NUM_TRACK; i++) {
                            int x = key_width + i * selecter_width;
                            if (vsq.Track.Count > i + 1) {
                                if (x <= e.X && e.X < x + selecter_width) {
                                    int new_selected = i + 1;
                                    if (EditorManager.Selected != new_selected) {
                                        EditorManager.Selected = i + 1;
                                        try {
                                            if (SelectedTrackChanged != null) {
                                                SelectedTrackChanged.Invoke(this, i + 1);
                                            }
                                        } catch (Exception ex) {
                                            Logger.StdErr("TrackSelector#TrackSelector_MouseDown; ex=" + ex);
                                        }
                                        Invalidate();
                                        return;
                                    } else if (x + selecter_width - TS.PX_WIDTH_RENDER <= e.X && e.X < e.X + selecter_width) {
                                        if (EditorManager.getRenderRequired(EditorManager.Selected) && !EditorManager.isPlaying()) {
                                            try {
                                                if (RenderRequired != null) {
                                                    RenderRequired.Invoke(this, EditorManager.Selected);
                                                }
                                            } catch (Exception ex) {
                                                Logger.StdErr("TrackSelector#TrackSelector_MouseDown; ex=" + ex);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
            } else if (height - 2 * TS.OFFSET_TRACK_TAB <= e.Y && e.Y < height - TS.OFFSET_TRACK_TAB) {
                #region MouseDown occured on singer tab
                mMouseDownMode = MouseDownMode.SINGER_LIST;
                EditorManager.itemSelection.clearPoint();
                mMouseTracer.clear();
                VsqEvent ve = null;
                if (key_width <= e.X && e.X <= width) {
                    ve = findItemAt(e.X, e.Y);
                }
                if (EditorManager.SelectedTool == EditTool.ERASER) {
                    #region EditTool.Eraser
                    if (ve != null && ve.Clock > 0) {
                        CadenciiCommand run = new CadenciiCommand(VsqCommand.generateCommandEventDelete(selected, ve.InternalID));
                        executeCommand(run, true);
                    }
                    #endregion
                } else {
                    if (ve != null) {
                        if ((mModifierOnMouseDown & mModifierKey) == mModifierKey) {
                            if (EditorManager.itemSelection.isEventContains(EditorManager.Selected, ve.InternalID)) {
                                List<int> old = new List<int>();
                                foreach (var item in EditorManager.itemSelection.getEventIterator()) {
                                    int id = item.original.InternalID;
                                    if (id != ve.InternalID) {
                                        old.Add(id);
                                    }
                                }
                                EditorManager.itemSelection.clearEvent();
                                EditorManager.itemSelection.addEventAll(old);
                            } else {
                                EditorManager.itemSelection.addEvent(ve.InternalID);
                            }
                        } else if (((Keys) Control.ModifierKeys & Keys.Shift) == Keys.Shift) {
                            int last_clock = EditorManager.itemSelection.getLastEvent().original.Clock;
                            int tmin = Math.Min(ve.Clock, last_clock);
                            int tmax = Math.Max(ve.Clock, last_clock);
                            List<int> add_required = new List<int>();
                            for (Iterator<VsqEvent> itr = MusicManager.getVsqFile().Track[EditorManager.Selected].getEventIterator(); itr.hasNext(); ) {
                                VsqEvent item = itr.next();
                                if (item.ID.type == VsqIDType.Singer && tmin <= item.Clock && item.Clock <= tmax) {
                                    add_required.Add(item.InternalID);
                                    //EditorManager.AddSelectedEvent( item.InternalID );
                                }
                            }
                            add_required.Add(ve.InternalID);
                            EditorManager.itemSelection.addEventAll(add_required);
                        } else {
                            if (!EditorManager.itemSelection.isEventContains(EditorManager.Selected, ve.InternalID)) {
                                EditorManager.itemSelection.clearEvent();
                            }
                            EditorManager.itemSelection.addEvent(ve.InternalID);
                        }
                        mSingerMoveStartedClock = clock;
                    } else {
                        EditorManager.itemSelection.clearEvent();
                    }
                }
                #endregion
            } else {
                #region MouseDown occred on other position
                bool clock_inner_note = false; //マウスの降りたクロックが，ノートの範囲内かどうかをチェック
                int left_clock = EditorManager.clockFromXCoord(EditorManager.keyWidth);
                int right_clock = EditorManager.clockFromXCoord(getWidth());
                for (Iterator<VsqEvent> itr = vsq_track.getEventIterator(); itr.hasNext(); ) {
                    VsqEvent ve = itr.next();
                    if (ve.ID.type == VsqIDType.Anote) {
                        int start = ve.Clock;
                        if (right_clock < start) {
                            break;
                        }
                        int end = ve.Clock + ve.ID.getLength();
                        if (end < left_clock) {
                            continue;
                        }
                        if (start <= clock && clock < end) {
                            clock_inner_note = true;
                            break;
                        }
                    }
                }
#if DEBUG
                CDebug.WriteLine("    clock_inner_note=" + clock_inner_note);
#endif
                if (EditorManager.keyWidth <= e.X) {
                    if (e.Button == MouseButtons.Left && !mSpaceKeyDowned) {
                        mMouseDownMode = MouseDownMode.CURVE_EDIT;
                        int quantized_clock = clock;
                        int unit = EditorManager.getPositionQuantizeClock();
                        int odd = clock % unit;
                        quantized_clock -= odd;
                        if (odd > unit / 2) {
                            quantized_clock += unit;
                        }

                        int px_shift = TS.DOT_WID + EditorManager.editorConfig.PxToleranceBezier;
                        int px_width = px_shift * 2 + 1;

                        if (EditorManager.SelectedTool == EditTool.LINE) {
                            #region Line
                            if (EditorManager.isCurveMode()) {
                                if (mSelectedCurve.equals(CurveType.Env)) {
                                    if (processMouseDownEnvelope(e)) {
                                        Invalidate();
                                        return;
                                    }
                                    if (processMouseDownPreutteranceAndOverlap(e)) {
                                        Invalidate();
                                        return;
                                    }
                                } else if (!mSelectedCurve.equals(CurveType.VEL) &&
                                            !mSelectedCurve.equals(CurveType.Accent) &&
                                            !mSelectedCurve.equals(CurveType.Decay) &&
                                            !mSelectedCurve.equals(CurveType.Env)) {
                                    if (processMouseDownBezier(e)) {
                                        Invalidate();
                                        return;
                                    }
                                }
                            } else {
                                if (mSelectedCurve.equals(CurveType.Env)) {
                                    if (processMouseDownEnvelope(e)) {
                                        Invalidate();
                                        return;
                                    }
                                    if (processMouseDownPreutteranceAndOverlap(e)) {
                                        Invalidate();
                                        return;
                                    }
                                }
                            }
                            mMouseTracer.clear();
                            mMouseTracer.appendFirst(e.X + stdx, e.Y);
                            #endregion
                        } else if (EditorManager.SelectedTool == EditTool.PENCIL) {
                            #region Pencil
                            if (EditorManager.isCurveMode()) {
                                #region CurveMode
                                if (mSelectedCurve.equals(CurveType.VibratoRate) || mSelectedCurve.equals(CurveType.VibratoDepth)) {
                                    // todo: TrackSelector_MouseDownのベジエ曲線
                                } else if (mSelectedCurve.equals(CurveType.Env)) {
                                    if (processMouseDownEnvelope(e)) {
                                        Invalidate();
                                        return;
                                    }
                                    if (processMouseDownPreutteranceAndOverlap(e)) {
                                        Invalidate();
                                        return;
                                    }
                                } else if (!mSelectedCurve.equals(CurveType.VEL) &&
                                            !mSelectedCurve.equals(CurveType.Accent) &&
                                            !mSelectedCurve.equals(CurveType.Decay) &&
                                            !mSelectedCurve.equals(CurveType.Env)) {
                                    if (processMouseDownBezier(e)) {
                                        Invalidate();
                                        return;
                                    }
                                } else {
                                    mMouseDownMode = MouseDownMode.NONE;
                                }
                                #endregion
                            } else {
                                #region NOT CurveMode
                                if (mSelectedCurve.equals(CurveType.Env)) {
                                    if (processMouseDownEnvelope(e)) {
                                        Invalidate();
                                        return;
                                    }
                                    if (processMouseDownPreutteranceAndOverlap(e)) {
                                        Invalidate();
                                        return;
                                    }
                                }
                                mMouseTracer.clear();
                                int x = e.X + EditorManager.MainWindow.Model.StartToDrawX;
                                mMouseTracer.appendFirst(x, e.Y);
                                mPencilMoved = false;

                                mMouseHoverThread = new Thread(new ThreadStart(MouseHoverEventGenerator));
                                mMouseHoverThread.Start();
                                #endregion
                            }
                            #endregion
                        } else if (EditorManager.SelectedTool == EditTool.ARROW) {
                            #region Arrow
                            bool found = false;
                            if (mSelectedCurve.isScalar() || mSelectedCurve.isAttachNote()) {
                                if (mSelectedCurve.equals(CurveType.Env)) {
                                    if (processMouseDownEnvelope(e)) {
                                        Invalidate();
                                        return;
                                    }
                                    if (processMouseDownPreutteranceAndOverlap(e)) {
                                        Invalidate();
                                        return;
                                    }
                                }
                                mMouseDownMode = MouseDownMode.NONE;
                            } else {
                                // まずベジエ曲線の点にヒットしてないかどうかを検査
                                List<BezierChain> dict = MusicManager.getVsqFile().AttachedCurves.get(EditorManager.Selected - 1).get(mSelectedCurve);
                                EditorManager.itemSelection.clearBezier();
                                for (int i = 0; i < dict.Count; i++) {
                                    BezierChain bc = dict[i];
                                    foreach (var bp in bc.points) {
                                        Point pt = getScreenCoord(bp.getBase());
                                        Rectangle rc = new Rectangle(pt.X - px_shift, pt.Y - px_shift, px_width, px_width);
                                        if (isInRect(e.X, e.Y, rc)) {
                                            EditorManager.itemSelection.addBezier(new SelectedBezierPoint(bc.id, bp.getID(), BezierPickedSide.BASE, bp));
                                            mEditingBezierOriginal = (BezierChain)bc.clone();
                                            found = true;
                                            break;
                                        }

                                        if (bp.getControlLeftType() != BezierControlType.None) {
                                            pt = getScreenCoord(bp.getControlLeft());
                                            rc = new Rectangle(pt.X - px_shift, pt.Y - px_shift, px_width, px_width);
                                            if (isInRect(e.X, e.Y, rc)) {
                                                EditorManager.itemSelection.addBezier(new SelectedBezierPoint(bc.id, bp.getID(), BezierPickedSide.LEFT, bp));
                                                mEditingBezierOriginal = (BezierChain)bc.clone();
                                                found = true;
                                                break;
                                            }
                                        }

                                        if (bp.getControlRightType() != BezierControlType.None) {
                                            pt = getScreenCoord(bp.getControlRight());
                                            rc = new Rectangle(pt.X - px_shift, pt.Y - px_shift, px_width, px_width);
                                            if (isInRect(e.X, e.Y, rc)) {
                                                EditorManager.itemSelection.addBezier(new SelectedBezierPoint(bc.id, bp.getID(), BezierPickedSide.RIGHT, bp));
                                                mEditingBezierOriginal = (BezierChain)bc.clone();
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (found) {
                                        break;
                                    }
                                }
                                if (found) {
                                    mMouseDownMode = MouseDownMode.BEZIER_MODE;
                                }
                            }

                            // ベジエ曲線の点にヒットしなかった場合
                            if (!found) {
                                #region NOT CurveMode
                                VsqEvent ve = findItemAt(e.X, e.Y);
                                // マウス位置の音符アイテムを検索
                                if (ve != null) {
                                    bool found2 = false;
                                    if ((mModifierOnMouseDown & mModifierKey) == mModifierKey) {
                                        // clicked with CTRL key
                                        List<int> list = new List<int>();
                                        foreach (var item in EditorManager.itemSelection.getEventIterator()) {
                                            VsqEvent ve2 = item.original;
                                            if (ve.InternalID == ve2.InternalID) {
                                                found2 = true;
                                            } else {
                                                list.Add(ve2.InternalID);
                                            }
                                        }
                                        EditorManager.itemSelection.clearEvent();
                                        EditorManager.itemSelection.addEventAll(list);
                                    } else if (((Keys) Control.ModifierKeys & Keys.Shift) == Keys.Shift) {
                                        // clicked with Shift key
                                        SelectedEventEntry last_selected = EditorManager.itemSelection.getLastEvent();
                                        if (last_selected != null) {
                                            int last_clock = last_selected.original.Clock;
                                            int tmin = Math.Min(ve.Clock, last_clock);
                                            int tmax = Math.Max(ve.Clock, last_clock);
                                            List<int> add_required = new List<int>();
                                            foreach (var item in MusicManager.getVsqFile().Track[EditorManager.Selected].getNoteEventIterator()) {
                                                if (tmin <= item.Clock && item.Clock <= tmax) {
                                                    add_required.Add(item.InternalID);
                                                }
                                            }
                                            EditorManager.itemSelection.addEventAll(add_required);
                                        }
                                    } else {
                                        // no modefier key
                                        if (!EditorManager.itemSelection.isEventContains(EditorManager.Selected, ve.InternalID)) {
                                            EditorManager.itemSelection.clearEvent();
                                        }
                                    }
                                    if (!found2) {
                                        EditorManager.itemSelection.addEvent(ve.InternalID);
                                    }

                                    mMouseDownMode = MouseDownMode.VEL_WAIT_HOVER;
                                    mVelEditLastSelectedID = ve.InternalID;
                                    if (mSelectedCurve.equals(CurveType.VEL)) {
                                        if (EditorManager.mDrawIsUtau[selected - 1]) {
                                            mVelEditShiftY = e.Y - yCoordFromValue(ve.UstEvent == null ? 100 : ve.UstEvent.getIntensity());
                                        } else {
                                            mVelEditShiftY = e.Y - yCoordFromValue(ve.ID.Dynamics);
                                        }
                                    } else if (mSelectedCurve.equals(CurveType.Accent)) {
                                        mVelEditShiftY = e.Y - yCoordFromValue(ve.ID.DEMaccent);
                                    } else if (mSelectedCurve.equals(CurveType.Decay)) {
                                        mVelEditShiftY = e.Y - yCoordFromValue(ve.ID.DEMdecGainRate);
                                    }
                                    mVelEditSelected.Clear();
                                    if (EditorManager.itemSelection.isEventContains(EditorManager.Selected, mVelEditLastSelectedID)) {
                                        foreach (var item in EditorManager.itemSelection.getEventIterator()) {
                                            mVelEditSelected[item.original.InternalID] =
                                                                    new SelectedEventEntry(EditorManager.Selected,
                                                                                            item.original,
                                                                                            item.editing);
                                        }
                                    } else {
                                        mVelEditSelected[mVelEditLastSelectedID] =
                                                                new SelectedEventEntry(EditorManager.Selected,
                                                                                        (VsqEvent)ve.clone(),
                                                                                        (VsqEvent)ve.clone());
                                    }
                                    mMouseHoverThread = new Thread(new ThreadStart(MouseHoverEventGenerator));
                                    mMouseHoverThread.Start();
                                    Invalidate();
                                    return;
                                }

                                // マウス位置のデータポイントを検索
                                long id = findDataPointAt(e.X, e.Y);
                                if (id > 0) {
                                    if (EditorManager.itemSelection.isPointContains(id)) {
                                        if ((mModifierOnMouseDown & mModifierKey) == mModifierKey) {
                                            EditorManager.itemSelection.removePoint(id);
                                            mMouseDownMode = MouseDownMode.NONE;
                                            Invalidate();
                                            return;
                                        }
                                    } else {
                                        if ((mModifierOnMouseDown & mModifierKey) != mModifierKey) {
                                            EditorManager.itemSelection.clearPoint();
                                        }
                                        EditorManager.itemSelection.addPoint(mSelectedCurve, id);
                                    }

                                    mMouseDownMode = MouseDownMode.POINT_MOVE;
                                    mMovingPoints.Clear();
                                    VsqBPList list = MusicManager.getVsqFile().Track[EditorManager.Selected].getCurve(mSelectedCurve.getName());
                                    if (list != null) {
                                        int count = list.size();
                                        for (int i = 0; i < count; i++) {
                                            VsqBPPair item = list.getElementB(i);
                                            if (EditorManager.itemSelection.isPointContains(item.id)) {
                                                mMovingPoints.Add(new BPPair(list.getKeyClock(i), item.value));
                                            }
                                        }
                                        Invalidate();
                                        return;
                                    }
                                } else {
                                    if ((mModifierOnMouseDown & Keys.Control) != Keys.Control) {
                                        EditorManager.itemSelection.clearPoint();
                                    }
                                    if ((mModifierOnMouseDown & Keys.Shift) != Keys.Shift && (mModifierOnMouseDown & mModifierKey) != mModifierKey) {
                                        EditorManager.itemSelection.clearPoint();
                                    }
                                }

                                if ((mModifierOnMouseDown & mModifierKey) != mModifierKey) {
                                    EditorManager.IsCurveSelectedIntervalEnabled = false;
                                }
								if (EditorManager.editorConfig.CurveSelectingQuantized) {
                                    EditorManager.mCurveSelectingRectangle = new Rectangle(quantized_clock, value, 0, 0);
                                } else {
                                    EditorManager.mCurveSelectingRectangle = new Rectangle(clock, value, 0, 0);
                                }
                                #endregion
                            }
                            #endregion
                        } else if (EditorManager.SelectedTool == EditTool.ERASER) {
                            #region Eraser
                            VsqEvent ve3 = findItemAt(e.X, e.Y);
                            if (ve3 != null) {
                                EditorManager.itemSelection.clearEvent();
                                CadenciiCommand run = new CadenciiCommand(VsqCommand.generateCommandEventDelete(selected,
                                                                                                                  ve3.InternalID));
                                executeCommand(run, true);
                            } else {
                                if (EditorManager.isCurveMode()) {
                                    List<BezierChain> list = vsq.AttachedCurves.get(EditorManager.Selected - 1).get(mSelectedCurve);
                                    if (list != null) {
                                        ByRef<BezierChain> chain = new ByRef<BezierChain>();
                                        ByRef<BezierPoint> point = new ByRef<BezierPoint>();
                                        ByRef<BezierPickedSide> side = new ByRef<BezierPickedSide>();
                                        findBezierPointAt(e.X, e.Y, list, chain, point, side, TS.DOT_WID, EditorManager.editorConfig.PxToleranceBezier);
                                        if (point.value != null) {
                                            if (side.value == BezierPickedSide.BASE) {
                                                // データ点自体を削除
                                                BezierChain work = (BezierChain)chain.value.clone();
                                                int count = work.points.Count;
                                                if (count > 1) {
                                                    // 2個以上のデータ点があるので、BezierChainを置換
                                                    for (int i = 0; i < count; i++) {
                                                        BezierPoint bp = work.points[i];
                                                        if (bp.getID() == point.value.getID()) {
                                                            work.points.RemoveAt(i);
                                                            break;
                                                        }
                                                    }
                                                    CadenciiCommand run = VsqFileEx.generateCommandReplaceBezierChain(selected,
                                                                                                                       mSelectedCurve,
                                                                                                                       chain.value.id,
                                                                                                                       work,
                                                                                                                       ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                                                    executeCommand(run, true);
                                                    mMouseDownMode = MouseDownMode.NONE;
                                                    Invalidate();
                                                    return;
                                                } else {
                                                    // 1個しかデータ点がないので、BezierChainを削除
                                                    CadenciiCommand run = VsqFileEx.generateCommandDeleteBezierChain(EditorManager.Selected,
                                                                                                                      mSelectedCurve,
                                                                                                                      chain.value.id,
                                                                                                                      ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                                                    executeCommand(run, true);
                                                    mMouseDownMode = MouseDownMode.NONE;
                                                    Invalidate();
                                                    return;
                                                }
                                            } else {
                                                // 滑らかにするオプションを解除する
                                                BezierChain work = (BezierChain)chain.value.clone();
                                                int count = work.points.Count;
                                                for (int i = 0; i < count; i++) {
                                                    BezierPoint bp = work.points[i];
                                                    if (bp.getID() == point.value.getID()) {
                                                        bp.setControlLeftType(BezierControlType.None);
                                                        bp.setControlRightType(BezierControlType.None);
                                                        break;
                                                    }
                                                }
                                                CadenciiCommand run = VsqFileEx.generateCommandReplaceBezierChain(EditorManager.Selected,
                                                                                                                   mSelectedCurve,
                                                                                                                   chain.value.id,
                                                                                                                   work,
                                                                                                                   ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                                                executeCommand(run, true);
                                                mMouseDownMode = MouseDownMode.NONE;
                                                Invalidate();
                                                return;
                                            }
                                        }
                                    }
                                } else {
                                    long id = findDataPointAt(e.X, e.Y);
                                    if (id > 0) {
                                        VsqBPList item = MusicManager.getVsqFile().Track[EditorManager.Selected].getCurve(mSelectedCurve.getName());
                                        if (item != null) {
                                            VsqBPList work = (VsqBPList)item.clone();
                                            VsqBPPairSearchContext context = work.findElement(id);
                                            if (context.point.id == id) {
                                                work.remove(context.clock);
                                                CadenciiCommand run = new CadenciiCommand(
                                                    VsqCommand.generateCommandTrackCurveReplace(selected,
                                                                                                 mSelectedCurve.getName(),
                                                                                                 work));
                                                executeCommand(run, true);
                                                mMouseDownMode = MouseDownMode.NONE;
                                                Invalidate();
                                                return;
                                            }
                                        }
                                    }
                                }

                                if ((mModifierOnMouseDown & Keys.Shift) != Keys.Shift && (mModifierOnMouseDown & mModifierKey) != mModifierKey) {
                                    EditorManager.itemSelection.clearPoint();
                                }
								if (EditorManager.editorConfig.CurveSelectingQuantized) {
                                    EditorManager.mCurveSelectingRectangle = new Rectangle(quantized_clock, value, 0, 0);
                                } else {
                                    EditorManager.mCurveSelectingRectangle = new Rectangle(clock, value, 0, 0);
                                }
                            }
                            #endregion
                        }
                    } else if (e.Button == MouseButtons.Right) {
                        if (EditorManager.isCurveMode()) {
                            if (!mSelectedCurve.equals(CurveType.VEL) && !mSelectedCurve.equals(CurveType.Env)) {
                                List<BezierChain> dict = MusicManager.getVsqFile().AttachedCurves.get(EditorManager.Selected - 1).get(mSelectedCurve);
                                EditorManager.itemSelection.clearBezier();
                                bool found = false;
                                for (int i = 0; i < dict.Count; i++) {
                                    BezierChain bc = dict[i];
                                    foreach (var bp in bc.points) {
                                        Point pt = getScreenCoord(bp.getBase());
                                        Rectangle rc = new Rectangle(pt.X - TS.DOT_WID, pt.Y - TS.DOT_WID, 2 * TS.DOT_WID + 1, 2 * TS.DOT_WID + 1);
                                        if (isInRect(e.X, e.Y, rc)) {
                                            EditorManager.itemSelection.addBezier(new SelectedBezierPoint(bc.id, bp.getID(), BezierPickedSide.BASE, bp));
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (found) {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                } else {
                    EditorManager.IsCurveSelectedIntervalEnabled = false;
                }
                #endregion
            }
            Invalidate();
        }

        private bool processMouseDownBezier(MouseEventArgs e)
        {
#if DEBUG
            Logger.StdOut("TrackSelector::processMouseDownBezier");
#endif
            int clock = EditorManager.clockFromXCoord(e.X);
            int max = mSelectedCurve.getMaximum();
            int min = mSelectedCurve.getMinimum();
            int value = valueFromYCoord(e.Y);
            if (value < min) {
                value = min;
            } else if (max < value) {
                value = max;
            }
            int px_shift = TS.DOT_WID + EditorManager.editorConfig.PxToleranceBezier;
            int px_width = px_shift * 2 + 1;
            Keys modifier = (Keys) Control.ModifierKeys;

            int track = EditorManager.Selected;
            bool too_near = false; // clicked position is too near to existing bezier points
            bool is_middle = false;

            // check whether bezier point exists on clicked position
            List<BezierChain> dict = MusicManager.getVsqFile().AttachedCurves.get(track - 1).get(mSelectedCurve);
            ByRef<BezierChain> found_chain = new ByRef<BezierChain>();
            ByRef<BezierPoint> found_point = new ByRef<BezierPoint>();
            ByRef<BezierPickedSide> found_side = new ByRef<BezierPickedSide>();
            findBezierPointAt(
                e.X, e.Y,
                dict, found_chain, found_point, found_side,
                TS.DOT_WID, EditorManager.editorConfig.PxToleranceBezier);
#if DEBUG
            Logger.StdOut("TrackSelector::processMouseDownBezier; (found_chain.value==null)=" + (found_chain.value == null));
#endif

            if (found_chain.value != null) {
                EditorManager.itemSelection.addBezier(
                    new SelectedBezierPoint(
                        found_chain.value.id, found_point.value.getID(),
                        found_side.value, found_point.value));
                mEditingBezierOriginal = (BezierChain)found_chain.value.clone();
                mMouseDownMode = MouseDownMode.BEZIER_MODE;
            } else {
                if (EditorManager.SelectedTool != EditTool.PENCIL) {
                    return false;
                }
                BezierChain target_chain = null;
                for (int j = 0; j < dict.Count; j++) {
                    BezierChain bc = dict[j];
                    for (int i = 1; i < bc.size(); i++) {
                        if (!is_middle && bc.points[i - 1].getBase().getX() <= clock && clock <= bc.points[i].getBase().getX()) {
                            target_chain = (BezierChain)bc.clone();
                            is_middle = true;
                        }
                        if (!too_near) {
                            foreach (var bp in bc.points) {
                                Point pt = getScreenCoord(bp.getBase());
                                Rectangle rc = new Rectangle(pt.X - px_shift, pt.Y - px_shift, px_width, px_width);
                                if (isInRect(e.X, e.Y, rc)) {
                                    too_near = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!too_near) {
                    if ((modifier & mModifierKey) != mModifierKey && target_chain == null) {
                        // search BezierChain just before the clicked position
                        int tmax = -1;
                        for (int i = 0; i < dict.Count; i++) {
                            BezierChain bc = dict[i];
                            bc.points.Sort();
                            // check most nearest data point from clicked position
                            int last = (int)bc.points[bc.points.Count - 1].getBase().getX();
                            if (tmax < last && last < clock) {
                                tmax = last;
                                target_chain = (BezierChain)bc.clone();
                            }
                        }
                    }

#if DEBUG
                    CDebug.WriteLine("    (target_chain==null)=" + (target_chain == null));
                    if (target_chain != null && found_point.value != null) {
                        Logger.StdOut("TrackSelector::procesMouseDownBezier; before:(" + found_point.value.getPosition(found_side.value) + "," + found_point.value.getPosition(found_side.value) + "); after:(" + clock + "," + value + ")");
                    }
#endif
                    // fork whether target_chain is null or not
                    PointD pt = new PointD(clock, value);
                    BezierPoint bp = null;
                    int chain_id = -1;
                    int point_id = -1;
                    if (target_chain == null) {
                        // generate new BezierChain
                        BezierChain adding = new BezierChain();
                        bp = new BezierPoint(pt, pt, pt);
                        point_id = adding.getNextId();
                        bp.setID(point_id);
                        adding.add(bp);
                        chain_id = MusicManager.getVsqFile().AttachedCurves.get(track - 1).getNextId(mSelectedCurve);
#if DEBUG
                        CDebug.WriteLine("    new chain_id=" + chain_id);
#endif
                        CadenciiCommand run = VsqFileEx.generateCommandAddBezierChain(track,
                                                                                mSelectedCurve,
                                                                                chain_id,
                                                                                ApplicationGlobal.appConfig.getControlCurveResolutionValue(),
                                                                                adding);
                        executeCommand(run, false);
                        mMouseDownMode = MouseDownMode.BEZIER_ADD_NEW;
                    } else {
                        mEditingBezierOriginal = (BezierChain)target_chain.clone();
                        bp = new BezierPoint(pt, pt, pt);
                        point_id = target_chain.getNextId();
                        bp.setID(point_id);
                        target_chain.add(bp);
                        target_chain.points.Sort();
                        chain_id = target_chain.id;
                        CadenciiCommand run = VsqFileEx.generateCommandReplaceBezierChain(track,
                                                                                    mSelectedCurve,
                                                                                    target_chain.id,
                                                                                    target_chain,
                                                                                    ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                        executeCommand(run, false);
                        mMouseDownMode = MouseDownMode.BEZIER_EDIT;
                    }
                    EditorManager.itemSelection.clearBezier();
                    EditorManager.itemSelection.addBezier(new SelectedBezierPoint(chain_id, point_id, BezierPickedSide.BASE, bp));
                } else {
                    mMouseDownMode = MouseDownMode.NONE;
                }
            }
            return true;
        }

        private bool processMouseDownPreutteranceAndOverlap(MouseEventArgs e)
        {
            ByRef<int> internal_id = new ByRef<int>();
            ByRef<Boolean> found_flag_was_overlap = new ByRef<Boolean>();
            if (findPreUtteranceOrOverlapAt(e.X, e.Y, internal_id, found_flag_was_overlap)) {
                if (found_flag_was_overlap.value) {
                    mOverlapEditingID = internal_id.value;
                    mPreOverlapEditing = MusicManager.getVsqFile().Track[EditorManager.Selected].findEventFromID(mOverlapEditingID);
                    if (mPreOverlapEditing == null) {
                        mMouseDownMode = MouseDownMode.NONE;
                        return false;
                    }
                    mPreOverlapOriginal = (VsqEvent)mPreOverlapEditing.clone();
                    mMouseDownMode = MouseDownMode.OVERLAP_MOVE;
                    return true;
                } else {
                    mPreUtteranceEditingID = internal_id.value;
                    mPreOverlapEditing = MusicManager.getVsqFile().Track[EditorManager.Selected].findEventFromID(mPreUtteranceEditingID);
                    if (mPreOverlapEditing == null) {
                        mMouseDownMode = MouseDownMode.NONE;
                        return false;
                    }
                    mPreOverlapOriginal = (VsqEvent)mPreOverlapEditing.clone();
                    mMouseDownMode = MouseDownMode.PRE_UTTERANCE_MOVE;
                    return true;
                }
            }
            return false;
        }

        private bool processMouseDownEnvelope(MouseEventArgs e)
        {
            ByRef<int> internal_id = new ByRef<int>(-1);
            ByRef<int> point_kind = new ByRef<int>(-1);
            if (!findEnvelopePointAt(e.X, e.Y, internal_id, point_kind)) {
                return false;
            }
#if DEBUG
            Logger.StdOut("processTrackSelectorMouseDownForEnvelope; internal_id=" + internal_id.value + "; point_kind=" + point_kind.value);
#endif
            mEnvelopeOriginal = null;
            VsqFileEx vsq = MusicManager.getVsqFile();
            VsqTrack vsq_track = vsq.Track[EditorManager.Selected];
            VsqEvent found = vsq_track.findEventFromID(internal_id.value);
            if (found == null) {
                return false;
            }
            if (found.UstEvent != null && found.UstEvent.getEnvelope() != null) {
                mEnvelopeOriginal = (UstEnvelope)found.UstEvent.getEnvelope().clone();
                mEnvelopeEditing = found.UstEvent.getEnvelope();
            }
            if (mEnvelopeOriginal == null) {
                found.UstEvent.setEnvelope(new UstEnvelope());
                mEnvelopeEditing = found.UstEvent.getEnvelope();
                mEnvelopeOriginal = (UstEnvelope)found.UstEvent.getEnvelope().clone();
            }
            mMouseDownMode = MouseDownMode.ENVELOPE_MOVE;
            mEnvelopeEdigintID = internal_id.value;
            mEnvelopePointKind = point_kind.value;

            // エンベロープ点が移動可能な範囲を、あらかじめ取得
            // 描画される位置を取得
            VsqEvent item_prev = null;
            VsqEvent item = null;
            VsqEvent item_next = null;
            IEnumerator<VsqEvent> itr = vsq_track.getNoteEventIterator().GetEnumerator();
            while (true) {
                item_prev = item;
                item = item_next;
                if (itr.MoveNext()) {
                    item_next = itr.Current;
                } else {
                    item_next = null;
                }
                if (item_prev == null && item == null && item_next == null) {
                    break;
                }
                if (item == null) {
                    continue;
                }
                if (item.InternalID == mEnvelopeEdigintID) {
                    break;
                }
            }
            if (item_prev != null) {
                // 直前の音符と接触しているかどうか
                if (item_prev.Clock + item_prev.ID.getLength() < item.Clock) {
                    item_prev = null;
                }
            }
            if (item_next != null) {
                // 直後の音符と接触しているかどうか
                if (item.Clock + item.ID.getLength() < item_next.Clock) {
                    item_next = null;
                }
            }
            ByRef<Double> env_start = new ByRef<Double>(0.0);
            ByRef<Double> env_end = new ByRef<Double>(0.0);
            getEnvelopeRegion(vsq.TempoTable, item_prev, item, item_next, env_start, env_end);

            mEnvelopeRangeBegin = env_start.value;
            mEnvelopeRangeEnd = env_end.value;
            if (mEnvelopePointKind == 1) {
                mEnvelopeDotBegin = mEnvelopeRangeBegin;
                mEnvelopeDotEnd = mEnvelopeRangeEnd - (mEnvelopeOriginal.p4 + mEnvelopeOriginal.p3 + mEnvelopeOriginal.p5 + mEnvelopeOriginal.p2) / 1000.0;
            } else if (mEnvelopePointKind == 2) {
                mEnvelopeDotBegin = mEnvelopeRangeBegin + mEnvelopeOriginal.p1 / 1000.0;
                mEnvelopeDotEnd = mEnvelopeRangeEnd - (mEnvelopeOriginal.p4 + mEnvelopeOriginal.p3 + mEnvelopeOriginal.p5) / 1000.0;
            } else if (mEnvelopePointKind == 3) {
                mEnvelopeDotBegin = mEnvelopeRangeBegin + (mEnvelopeOriginal.p1 + mEnvelopeOriginal.p2) / 1000.0;
                mEnvelopeDotEnd = mEnvelopeRangeEnd - (mEnvelopeOriginal.p4 + mEnvelopeOriginal.p3) / 1000.0;
            } else if (mEnvelopePointKind == 4) {
                mEnvelopeDotBegin = mEnvelopeRangeBegin + (mEnvelopeOriginal.p1 + mEnvelopeOriginal.p2 + mEnvelopeOriginal.p5) / 1000.0;
                mEnvelopeDotEnd = mEnvelopeRangeEnd - mEnvelopeOriginal.p4 / 1000.0;
            } else if (mEnvelopePointKind == 5) {
                mEnvelopeDotBegin = mEnvelopeRangeBegin + (mEnvelopeOriginal.p1 + mEnvelopeOriginal.p2 + mEnvelopeOriginal.p5 + mEnvelopeOriginal.p3) / 1000.0;
                mEnvelopeDotEnd = mEnvelopeRangeEnd;
            }
            return true;
        }

        private void changeCurve(CurveType curve)
        {
#if DEBUG
            Logger.StdOut("TrackSelector#changCurve; getViewingCurveCount()=" + mViewingCurves.Count);
#endif
            if (!mSelectedCurve.equals(curve)) {
                mLastSelectedCurve = mSelectedCurve;
                mSelectedCurve = curve;
                try {
                    if (SelectedCurveChanged != null) {
                        SelectedCurveChanged.Invoke(this, curve);
                    }
                } catch (Exception ex) {
                    Logger.StdErr("TrackSelector#changeCurve; ex=" + ex);
                }
            }
        }

        private static bool isInRect(int x, int y, Rectangle rc)
        {
            if (rc.X <= x && x <= rc.X + rc.Width) {
                if (rc.Y <= y && y <= rc.Y + rc.Height) {
                    return true;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }

        /// <summary>
        /// クリックされた位置にある音符イベントまたは歌手変更イベントを取得します
        /// </summary>
        /// <param name="location"></param>
        /// <param name="position_x"></param>
        /// <returns></returns>
        private VsqEvent findItemAt(int locx, int locy)
        {
            if (MusicManager.getVsqFile() == null) {
                return null;
            }
            VsqTrack target = MusicManager.getVsqFile().Track[EditorManager.Selected];
            int count = target.getEventCount();
            for (int i = 0; i < count; i++) {
                VsqEvent ve = target.getEvent(i);
                if (ve.ID.type == VsqIDType.Singer) {
                    int x = EditorManager.xCoordFromClocks(ve.Clock);
                    if (getHeight() - 2 * TS.OFFSET_TRACK_TAB <= locy &&
                         locy <= getHeight() - TS.OFFSET_TRACK_TAB &&
                         x <= locx && locx <= x + TS.SINGER_ITEM_WIDTH) {
                        return ve;
                    } else if (getWidth() < x) {
                        //return null;
                    }
                } else if (ve.ID.type == VsqIDType.Anote) {
                    int x = EditorManager.xCoordFromClocks(ve.Clock);
                    int y = 0;
                    if (mSelectedCurve.equals(CurveType.VEL)) {
                        y = yCoordFromValue(ve.ID.Dynamics);
                    } else if (mSelectedCurve.equals(CurveType.Accent)) {
                        y = yCoordFromValue(ve.ID.DEMaccent);
                    } else if (mSelectedCurve.equals(CurveType.Decay)) {
                        y = yCoordFromValue(ve.ID.DEMdecGainRate);
                    } else {
                        continue;
                    }
                    if (0 <= locy && locy <= getHeight() - 2 * TS.OFFSET_TRACK_TAB &&
                         EditorManager.keyWidth <= locx && locx <= getWidth()) {
                    if (y <= locy && locy <= getHeight() - TS.FOOTER && x <= locx && locx <= x + TS.VEL_BAR_WIDTH) {
                            return ve;
                        }
                    }
                }
            }
            return null;
        }

        public void onMouseUp(Object sender, MouseEventArgs e)
        {
#if DEBUG
            CDebug.WriteLine("TrackSelector_MouseUp");
#endif
            mMouseDowned = false;
            if (mMouseHoverThread != null) {
                if (mMouseHoverThread.IsAlive) {
                    mMouseHoverThread.Abort();
                }
            }

            if (!mCurveVisible) {
                mMouseDownMode = MouseDownMode.NONE;
                Invalidate();
                return;
            }

            int selected = EditorManager.Selected;
            bool is_utau_mode = EditorManager.mDrawIsUtau[selected - 1];
            int stdx = EditorManager.MainWindow.Model.StartToDrawX;

            int max = mSelectedCurve.getMaximum();
            int min = mSelectedCurve.getMinimum();
            VsqFileEx vsq = MusicManager.getVsqFile();
            VsqTrack vsq_track = vsq.Track[selected];
#if DEBUG
            CDebug.WriteLine("    max,min=" + max + "," + min);
#endif
            if (mMouseDownMode == MouseDownMode.BEZIER_ADD_NEW ||
                 mMouseDownMode == MouseDownMode.BEZIER_MODE ||
                 mMouseDownMode == MouseDownMode.BEZIER_EDIT) {
                if (e.Button == MouseButtons.Left && sender is TrackSelector) {
                    int chain_id = EditorManager.itemSelection.getLastBezier().chainID;
                    BezierChain edited = (BezierChain)vsq.AttachedCurves.get(selected - 1).getBezierChain(mSelectedCurve, chain_id).clone();
                    if (mMouseDownMode == MouseDownMode.BEZIER_ADD_NEW) {
                        edited.id = chain_id;
                        CadenciiCommand pre = VsqFileEx.generateCommandDeleteBezierChain(selected,
                                                                                          mSelectedCurve,
                                                                                          chain_id,
                                                                                          ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                        executeCommand(pre, false);
                        CadenciiCommand run = VsqFileEx.generateCommandAddBezierChain(selected,
                                                                                       mSelectedCurve,
                                                                                       chain_id,
                                                                                       ApplicationGlobal.appConfig.getControlCurveResolutionValue(),
                                                                                       edited);
                        executeCommand(run, true);
                    } else if (mMouseDownMode == MouseDownMode.BEZIER_EDIT) {
                        CadenciiCommand pre = VsqFileEx.generateCommandReplaceBezierChain(selected,
                                                                                           mSelectedCurve,
                                                                                           chain_id,
                                                                                           mEditingBezierOriginal,
                                                                                           ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                        executeCommand(pre, false);
                        CadenciiCommand run = VsqFileEx.generateCommandReplaceBezierChain(selected,
                                                                                           mSelectedCurve,
                                                                                           chain_id,
                                                                                           edited,
                                                                                           ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                        executeCommand(run, true);
                    } else if (mMouseDownMode == MouseDownMode.BEZIER_MODE && mMouseMoved) {
                        vsq.AttachedCurves.get(selected - 1).setBezierChain(mSelectedCurve, chain_id, mEditingBezierOriginal);
                        CadenciiCommand run = VsqFileEx.generateCommandReplaceBezierChain(selected,
                                                                                           mSelectedCurve,
                                                                                           chain_id,
                                                                                           edited,
                                                                                           ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                        executeCommand(run, true);
#if DEBUG
                        CDebug.WriteLine("    m_mouse_down_mode=" + mMouseDownMode);
                        CDebug.WriteLine("    chain_id=" + chain_id);
#endif

                    }
                }
            } else if (mMouseDownMode == MouseDownMode.CURVE_EDIT ||
                      mMouseDownMode == MouseDownMode.VEL_WAIT_HOVER) {
                if (e.Button == MouseButtons.Left) {
                    if (EditorManager.SelectedTool == EditTool.ARROW) {
                        #region Arrow
                        if (mSelectedCurve.equals(CurveType.Env)) {

                        } else if (!mSelectedCurve.equals(CurveType.VEL) && !mSelectedCurve.equals(CurveType.Accent) && !mSelectedCurve.equals(CurveType.Decay)) {
                            if (EditorManager.mCurveSelectingRectangle.Width == 0) {
                                EditorManager.IsCurveSelectedIntervalEnabled = false;
                            } else {
                                if (!EditorManager.IsCurveSelectedIntervalEnabled) {
                                    int start = Math.Min(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                    int end = Math.Max(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                    EditorManager.mCurveSelectedInterval = new SelectedRegion(start);
                                    EditorManager.mCurveSelectedInterval.setEnd(end);
#if DEBUG
                                    CDebug.WriteLine("TrackSelector#TrackSelector_MouseUp; selected_region is set to TRUE");
#endif
                                    EditorManager.IsCurveSelectedIntervalEnabled = true;
                                } else {
                                    int start = Math.Min(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                    int end = Math.Max(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                    int old_start = EditorManager.mCurveSelectedInterval.getStart();
                                    int old_end = EditorManager.mCurveSelectedInterval.getEnd();
                                    EditorManager.mCurveSelectedInterval = new SelectedRegion(Math.Min(start, old_start));
                                    EditorManager.mCurveSelectedInterval.setEnd(Math.Max(end, old_end));
                                }

                                if ((mModifierOnMouseDown & Keys.Control) != Keys.Control) {
#if DEBUG
                                    Logger.StdOut("TrackSelector#TrackSelector_MouseUp; CTRL was not pressed");
#endif
                                    EditorManager.itemSelection.clearPoint();
                                }
                                if (!mSelectedCurve.equals(CurveType.Accent) &&
                                     !mSelectedCurve.equals(CurveType.Decay) &&
                                     !mSelectedCurve.equals(CurveType.Env) &&
                                     !mSelectedCurve.equals(CurveType.VEL) &&
                                     !mSelectedCurve.equals(CurveType.VibratoDepth) &&
                                     !mSelectedCurve.equals(CurveType.VibratoRate)) {
                                    VsqBPList list = vsq_track.getCurve(mSelectedCurve.getName());
                                    int count = list.size();
                                    Rectangle rc = new Rectangle(Math.Min(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width),
                                                                  Math.Min(EditorManager.mCurveSelectingRectangle.Y, EditorManager.mCurveSelectingRectangle.Y + EditorManager.mCurveSelectingRectangle.Height),
                                                                  Math.Abs(EditorManager.mCurveSelectingRectangle.Width),
                                                                  Math.Abs(EditorManager.mCurveSelectingRectangle.Height));
#if DEBUG
									Logger.StdOut("TrackSelectro#TrackSelectro_MouseUp; rc={x=" + rc.X + ", y=" + rc.Y + ", width=" + rc.Width + ", height=" + rc.Height + "}");
#endif
                                    for (int i = 0; i < count; i++) {
                                        int clock = list.getKeyClock(i);
                                        VsqBPPair item = list.getElementB(i);
                                        if (isInRect(clock, item.value, rc)) {
#if DEBUG
                                            Logger.StdOut("TrackSelector#TrackSelectro_MosueUp; selected; clock=" + clock + "; id=" + item.id);
#endif
                                            EditorManager.itemSelection.addPoint(mSelectedCurve, item.id);
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                    } else if (EditorManager.SelectedTool == EditTool.ERASER) {
                        #region Eraser
                        if (EditorManager.isCurveMode()) {
                            List<BezierChain> list = vsq.AttachedCurves.get(selected - 1).get(mSelectedCurve);
                            if (list != null) {
                                int x = Math.Min(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                int y = Math.Min(EditorManager.mCurveSelectingRectangle.Y, EditorManager.mCurveSelectingRectangle.Y + EditorManager.mCurveSelectingRectangle.Height);
                                Rectangle rc = new Rectangle(x, y, Math.Abs(EditorManager.mCurveSelectingRectangle.Width), Math.Abs(EditorManager.mCurveSelectingRectangle.Height));

                                bool changed = false; //1箇所でも削除が実行されたらtrue

                                int count = list.Count;
                                List<BezierChain> work = new List<BezierChain>();
                                for (int i = 0; i < count; i++) {
                                    BezierChain chain = list[i];
                                    BezierChain chain_copy = new BezierChain();
                                    chain_copy.setColor(chain.getColor());
                                    chain_copy.Default = chain.Default;
                                    chain_copy.id = chain.id;
                                    int point_count = chain.points.Count;
                                    for (int j = 0; j < point_count; j++) {
                                        BezierPoint point = chain.points[j];
                                        Point basepoint = point.getBase().toPoint();
                                        Point ctrl_l = point.getControlLeft().toPoint();
                                        Point ctrl_r = point.getControlRight().toPoint();
                                        if (isInRect(basepoint.X, basepoint.Y, rc)) {
                                            // データ点が選択範囲に入っているので、追加しない
                                            changed = true;
                                            continue;
                                        } else {
                                            if ((point.getControlLeftType() != BezierControlType.None && isInRect(ctrl_l.X, ctrl_l.Y, rc)) ||
                                                 (point.getControlRightType() != BezierControlType.None && isInRect(ctrl_r.X, ctrl_r.Y, rc))) {
                                                // 制御点が選択範囲に入っているので、「滑らかにする」オプションを解除して追加
                                                BezierPoint point_copy = (BezierPoint)point.clone();
                                                point_copy.setControlLeftType(BezierControlType.None);
                                                point_copy.setControlRightType(BezierControlType.None);
                                                chain_copy.points.Add(point_copy);
                                                changed = true;
                                                continue;
                                            } else {
                                                // 選択範囲に入っていないので、普通に追加
                                                chain_copy.points.Add((BezierPoint)point.clone());
                                            }
                                        }
                                    }
                                    if (chain_copy.points.Count > 0) {
                                        work.Add(chain_copy);
                                    }
                                }
                                if (changed) {
                                    SortedDictionary<CurveType, List<BezierChain>> comm = new SortedDictionary<CurveType, List<BezierChain>>();
                                    comm[mSelectedCurve] = work;
                                    CadenciiCommand run = VsqFileEx.generateCommandReplaceAttachedCurveRange(selected, comm);
                                    executeCommand(run, true);
                                }
                            }
                        } else {
                            if (mSelectedCurve.equals(CurveType.VEL) || mSelectedCurve.equals(CurveType.Accent) || mSelectedCurve.equals(CurveType.Decay)) {
                                #region VEL Accent Delay
                                int start = Math.Min(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                int end = Math.Max(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                int old_start = EditorManager.mCurveSelectedInterval.getStart();
                                int old_end = EditorManager.mCurveSelectedInterval.getEnd();
                                EditorManager.mCurveSelectedInterval = new SelectedRegion(Math.Min(start, old_start));
                                EditorManager.mCurveSelectedInterval.setEnd(Math.Max(end, old_end));
                                EditorManager.itemSelection.clearEvent();
                                List<int> deleting = new List<int>();
                                foreach (var ev in vsq_track.getNoteEventIterator()) {
                                    if (start <= ev.Clock && ev.Clock <= end) {
                                        deleting.Add(ev.InternalID);
                                    }
                                }
                                if (deleting.Count > 0) {
                                    CadenciiCommand er_run = new CadenciiCommand(
                                        VsqCommand.generateCommandEventDeleteRange(selected, deleting));
                                    executeCommand(er_run, true);
                                }
                                #endregion
                            } else if (mSelectedCurve.equals(CurveType.VibratoRate) || mSelectedCurve.equals(CurveType.VibratoDepth)) {
                                #region VibratoRate ViratoDepth
                                int er_start = Math.Min(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                int er_end = Math.Max(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                List<int> internal_ids = new List<int>();
                                List<VsqID> items = new List<VsqID>();
                                foreach (var ve in vsq_track.getNoteEventIterator()) {
                                    if (ve.ID.VibratoHandle == null) {
                                        continue;
                                    }
                                    int cl_vib_start = ve.Clock + ve.ID.VibratoDelay;
                                    int cl_vib_length = ve.ID.getLength() - ve.ID.VibratoDelay;
                                    int cl_vib_end = cl_vib_start + cl_vib_length;
                                    int clear_start = int.MaxValue;
                                    int clear_end = int.MinValue;
                                    if (er_start < cl_vib_start && cl_vib_start < er_end && er_end <= cl_vib_end) {
                                        // cl_vib_startからer_endまでをリセット
                                        clear_start = cl_vib_start;
                                        clear_end = er_end;
                                    } else if (cl_vib_start <= er_start && er_end <= cl_vib_end) {
                                        // er_startからer_endまでをリセット
                                        clear_start = er_start;
                                        clear_end = er_end;
                                    } else if (cl_vib_start < er_start && er_start < cl_vib_end && cl_vib_end < er_end) {
                                        // er_startからcl_vib_endまでをリセット
                                        clear_start = er_start;
                                        clear_end = cl_vib_end;
                                    } else if (er_start < cl_vib_start && cl_vib_end < er_end) {
                                        // 全部リセット
                                        clear_start = cl_vib_start;
                                        clear_end = cl_vib_end;
                                    }
                                    if (clear_start < clear_end) {
                                        float f_clear_start = (clear_start - cl_vib_start) / (float)cl_vib_length;
                                        float f_clear_end = (clear_end - cl_vib_start) / (float)cl_vib_length;
                                        VsqID item = (VsqID)ve.ID.clone();
                                        VibratoBPList target = null;
                                        if (mSelectedCurve.equals(CurveType.VibratoDepth)) {
                                            target = item.VibratoHandle.getDepthBP();
                                        } else {
                                            target = item.VibratoHandle.getRateBP();
                                        }
                                        List<float> bpx = new List<float>();
                                        List<int> bpy = new List<int>();
                                        bool start_added = false;
                                        bool end_added = false;
                                        for (int i = 0; i < target.getCount(); i++) {
                                            VibratoBPPair vbpp = target.getElement(i);
                                            if (vbpp.X < f_clear_start) {
                                                bpx.Add(vbpp.X);
                                                bpy.Add(vbpp.Y);
                                            } else if (f_clear_start == vbpp.X) {
                                                bpx.Add(vbpp.X);
                                                bpy.Add(64);
                                                start_added = true;
                                            } else if (f_clear_start < vbpp.X && !start_added) {
                                                bpx.Add(f_clear_start);
                                                bpy.Add(64);
                                                start_added = true;
                                            } else if (f_clear_end == vbpp.X) {
                                                bpx.Add(vbpp.X);
                                                bpy.Add(vbpp.Y);
                                                end_added = true;
                                            } else if (f_clear_end < vbpp.X && !end_added) {
                                                int y = vbpp.Y;
                                                if (i > 0) {
                                                    y = target.getElement(i - 1).Y;
                                                }
                                                bpx.Add(f_clear_end);
                                                bpy.Add(y);
                                                end_added = true;
                                                bpx.Add(vbpp.X);
                                                bpy.Add(vbpp.Y);
                                            } else if (f_clear_end < vbpp.X) {
                                                bpx.Add(vbpp.X);
                                                bpy.Add(vbpp.Y);
                                            }
                                        }
                                        if (mSelectedCurve.equals(CurveType.VibratoDepth)) {
                                            item.VibratoHandle.setDepthBP(
                                                new VibratoBPList(
                                                    PortUtil.convertFloatArray(bpx.ToArray()),
                                                    PortUtil.convertIntArray(bpy.ToArray())));
                                        } else {
                                            item.VibratoHandle.setRateBP(
                                                new VibratoBPList(
                                                    PortUtil.convertFloatArray(bpx.ToArray()),
                                                    PortUtil.convertIntArray(bpy.ToArray())));
                                        }
                                        internal_ids.Add(ve.InternalID);
                                        items.Add(item);
                                    }
                                }
                                CadenciiCommand run = new CadenciiCommand(
                                    VsqCommand.generateCommandEventChangeIDContaintsRange(selected,
                                                                                           PortUtil.convertIntArray(internal_ids.ToArray()),
                                                                                           items.ToArray()));
                                executeCommand(run, true);
                                #endregion
                            } else if (mSelectedCurve.equals(CurveType.Env)) {

                            } else {
                                #region Other Curves
                                VsqBPList work = vsq_track.getCurve(mSelectedCurve.getName());

                                // 削除するべきデータ点のリストを作成
                                int x = Math.Min(EditorManager.mCurveSelectingRectangle.X, EditorManager.mCurveSelectingRectangle.X + EditorManager.mCurveSelectingRectangle.Width);
                                int y = Math.Min(EditorManager.mCurveSelectingRectangle.Y, EditorManager.mCurveSelectingRectangle.Y + EditorManager.mCurveSelectingRectangle.Height);
                                Rectangle rc = new Rectangle(x, y, Math.Abs(EditorManager.mCurveSelectingRectangle.Width), Math.Abs(EditorManager.mCurveSelectingRectangle.Height));
                                List<long> delete = new List<long>();
                                int count = work.size();
                                for (int i = 0; i < count; i++) {
                                    int clock = work.getKeyClock(i);
                                    VsqBPPair item = work.getElementB(i);
                                    if (isInRect(clock, item.value, rc)) {
                                        delete.Add(item.id);
                                    }
                                }

                                if (delete.Count > 0) {
                                    CadenciiCommand run_eraser = new CadenciiCommand(
                                        VsqCommand.generateCommandTrackCurveEdit2(selected, mSelectedCurve.getName(), delete, new SortedDictionary<int, VsqBPPair>()));
                                    executeCommand(run_eraser, true);
                                }
                                #endregion
                            }
                        }
                        #endregion
                    } else if (!EditorManager.isCurveMode() && (EditorManager.SelectedTool == EditTool.PENCIL || EditorManager.SelectedTool == EditTool.LINE)) {
                        #region Pencil & Line
                        mMouseTracer.append(e.X + stdx, e.Y);
                        if (mPencilMoved) {
                            if (mSelectedCurve.equals(CurveType.VEL) || mSelectedCurve.equals(CurveType.Accent) || mSelectedCurve.equals(CurveType.Decay)) {
                                #region VEL Accent Decay
                                int start = mMouseTracer.firstKey();
                                int end = mMouseTracer.lastKey();
                                start = EditorManager.clockFromXCoord(start - stdx);
                                end = EditorManager.clockFromXCoord(end - stdx);
#if DEBUG
                                CDebug.WriteLine("        start=" + start);
                                CDebug.WriteLine("        end=" + end);
#endif
                                SortedDictionary<int, int> velocity = new SortedDictionary<int, int>();
                                foreach (var ve in vsq_track.getNoteEventIterator()) {
                                    if (start <= ve.Clock && ve.Clock < end) {
                                        int i = -1;
                                        int lkey = 0;
                                        int lvalue = 0;
                                        int count = mMouseTracer.size();
                                        foreach (var p in mMouseTracer.iterator()) {
                                            i++;
                                            int key = p.X;
                                            int value = p.Y;
                                            if (i == 0) {
                                                lkey = key;
                                                lvalue = value;
                                                continue;
                                            }
                                            int key0 = lkey;
                                            int key1 = key;
                                            int key0_clock = EditorManager.clockFromXCoord(key0 - stdx);
                                            int key1_clock = EditorManager.clockFromXCoord(key1 - stdx);
#if DEBUG
                                            CDebug.WriteLine("        key0,key1=" + key0 + "," + key1);
#endif
                                            if (key0_clock < ve.Clock && ve.Clock < key1_clock) {
                                                int key0_value = valueFromYCoord(lvalue);
                                                int key1_value = valueFromYCoord(value);
                                                float a = (key1_value - key0_value) / (float)(key1_clock - key0_clock);
                                                float b = key0_value - a * key0_clock;
                                                int new_value = (int)(a * ve.Clock + b);
                                                velocity[ve.InternalID] = new_value;
                                            } else if (key0_clock == ve.Clock) {
                                                velocity[ve.InternalID] = valueFromYCoord(lvalue);
                                            } else if (key1_clock == ve.Clock) {
                                                velocity[ve.InternalID] = valueFromYCoord(value);
                                            }
                                            lkey = key;
                                            lvalue = value;
                                        }
                                    }
                                }
                                if (velocity.Count > 0) {
                                    List<ValuePair<int, int>> cpy = new List<ValuePair<int, int>>();
                                    foreach (var internal_id in velocity.Keys) {
                                        int value = (int)velocity[internal_id];
                                        cpy.Add(new ValuePair<int, int>(internal_id, value));
                                    }
                                    CadenciiCommand run = null;
                                    if (mSelectedCurve.equals(CurveType.VEL)) {
                                        if (is_utau_mode) {
                                            int size = velocity.Count;
                                            VsqEvent[] events = new VsqEvent[size];
                                            int i = 0;
                                            foreach (var internal_id in velocity.Keys) {
                                                VsqEvent item = (VsqEvent)vsq_track.findEventFromID(internal_id).clone();
                                                if (item.UstEvent == null) {
                                                    item.UstEvent = new UstEvent();
                                                }
                                                item.UstEvent.setIntensity(velocity[internal_id]);
                                                events[i] = item;
                                                i++;
                                            }
                                            run = new CadenciiCommand(
                                                VsqCommand.generateCommandEventReplaceRange(
                                                    selected, events));
                                        } else {
                                            run = new CadenciiCommand(
                                                VsqCommand.generateCommandEventChangeVelocity(selected, cpy));
                                        }
                                    } else if (mSelectedCurve.equals(CurveType.Accent)) {
                                        run = new CadenciiCommand(
                                            VsqCommand.generateCommandEventChangeAccent(selected, cpy));
                                    } else if (mSelectedCurve.equals(CurveType.Decay)) {
                                        run = new CadenciiCommand(
                                            VsqCommand.generateCommandEventChangeDecay(selected, cpy));
                                    }
                                    executeCommand(run, true);
                                }
                                #endregion
                            } else if (mSelectedCurve.equals(CurveType.VibratoRate) || mSelectedCurve.equals(CurveType.VibratoDepth)) {
                                #region VibratoRate || VibratoDepth
                                int step_clock = ApplicationGlobal.appConfig.getControlCurveResolutionValue();
                                int step_px = (int)(step_clock * EditorManager.MainWindow.Model.ScaleX);
                                if (step_px <= 0) {
                                    step_px = 1;
                                }
                                int start = mMouseTracer.firstKey();
                                int end = mMouseTracer.lastKey();
#if DEBUG
                                CDebug.WriteLine("    start,end=" + start + " " + end);
#endif
                                List<int> internal_ids = new List<int>();
                                List<VsqID> items = new List<VsqID>();
                                foreach (var ve in vsq_track.getNoteEventIterator()) {
                                    if (ve.ID.VibratoHandle == null) {
                                        continue;
                                    }
                                    int cl_vib_start = ve.Clock + ve.ID.VibratoDelay;
                                    float cl_vib_length = ve.ID.getLength() - ve.ID.VibratoDelay;

                                    // 仮想スクリーン上の、ビブラートの描画開始位置
                                    int vib_start = EditorManager.xCoordFromClocks(cl_vib_start) + stdx;

                                    // 仮想スクリーン上の、ビブラートの描画終了位置
                                    int vib_end = EditorManager.xCoordFromClocks(ve.Clock + ve.ID.getLength()) + stdx;

                                    // マウスのトレースと、ビブラートの描画範囲がオーバーラップしている部分を検出
                                    int chk_start = Math.Max(vib_start, start);
                                    int chk_end = Math.Min(vib_end, end);
                                    if (chk_end <= chk_start) {
                                        // オーバーラップしていないのでスキップ
                                        continue;
                                    }

                                    float add_min = (EditorManager.clockFromXCoord(chk_start - stdx) - cl_vib_start) / cl_vib_length;
                                    float add_max = (EditorManager.clockFromXCoord(chk_end - stdx) - cl_vib_start) / cl_vib_length;

                                    List<ValuePair<float, int>> edit = new List<ValuePair<float, int>>();
                                    int lclock = -2 * step_clock;
                                    ValuePair<float, int> first = null; // xの値が0以下の最大のデータ点
                                    ValuePair<float, int> last = null;//xの値が1以上の最小のデータ点
                                    foreach (var p in mMouseTracer.iterator()) {
                                        if (p.X < chk_start) {
                                            continue;
                                        } else if (chk_end < p.X) {
                                            break;
                                        }
                                        int clock = EditorManager.clockFromXCoord(p.X - stdx);
                                        if (clock - lclock < step_clock) {
                                            continue;
                                        }
                                        int val = valueFromYCoord(p.Y);
                                        if (val < min) {
                                            val = min;
                                        } else if (max < val) {
                                            val = max;
                                        }
                                        float x = (clock - cl_vib_start) / cl_vib_length;
                                        ValuePair<float, int> tmp = new ValuePair<float, int>(x, val);
                                        if (0.0f < x && x < 1.0f) {
                                            edit.Add(tmp);
                                        } else if (x <= 0.0f) {
                                            first = tmp;
                                        } else if (1.0f <= x && last != null) {
                                            last = tmp;
                                        }
                                        lclock = clock;
                                    }
                                    if (first != null) {
                                        first.setKey(0.0f);
                                        edit.Add(first);
                                    }
                                    if (last != null) {
                                        last.setKey(1.0f);
                                        edit.Add(last);
                                    }

                                    VibratoBPList target = null;
                                    if (mSelectedCurve.equals(CurveType.VibratoRate)) {
                                        target = ve.ID.VibratoHandle.getRateBP();
                                    } else {
                                        target = ve.ID.VibratoHandle.getDepthBP();
                                    }
                                    if (target.getCount() > 0) {
                                        for (int i = 0; i < target.getCount(); i++) {
                                            if (target.getElement(i).X < add_min || add_max < target.getElement(i).X) {
                                                edit.Add(new ValuePair<float, int>(target.getElement(i).X,
                                                                                         target.getElement(i).Y));
                                            }
                                        }
                                    }
                                    edit.Sort();
                                    VsqID id = (VsqID)ve.ID.clone();
                                    float[] bpx = new float[edit.Count];
                                    int[] bpy = new int[edit.Count];
                                    for (int i = 0; i < edit.Count; i++) {
                                        bpx[i] = edit[i].getKey();
                                        bpy[i] = edit[i].getValue();
                                    }
                                    if (mSelectedCurve.equals(CurveType.VibratoDepth)) {
                                        id.VibratoHandle.setDepthBP(new VibratoBPList(bpx, bpy));
                                    } else {
                                        id.VibratoHandle.setRateBP(new VibratoBPList(bpx, bpy));
                                    }
                                    internal_ids.Add(ve.InternalID);
                                    items.Add(id);
                                }
                                if (internal_ids.Count > 0) {
                                    CadenciiCommand run = new CadenciiCommand(
                                        VsqCommand.generateCommandEventChangeIDContaintsRange(selected,
                                                                                               PortUtil.convertIntArray(internal_ids.ToArray()),
                                                                                               items.ToArray()));
                                    executeCommand(run, true);
                                }
                                #endregion
                            } else if (mSelectedCurve.equals(CurveType.Env)) {
                                #region Env

                                #endregion
                            } else {
                                #region Other Curves
                                int track = selected;
                                int step_clock = ApplicationGlobal.appConfig.getControlCurveResolutionValue();
                                int step_px = (int)(step_clock * EditorManager.MainWindow.Model.ScaleX);
                                if (step_px <= 0) {
                                    step_px = 1;
                                }
                                int start = mMouseTracer.firstKey();
                                int end = mMouseTracer.lastKey();
                                int clock_start = EditorManager.clockFromXCoord(start - stdx);
                                int clock_end = EditorManager.clockFromXCoord(end - stdx);
                                int last = start;

#if DEBUG
                                Logger.StdOut("TrackSelector#TrackSelector_MouseUp; start, end=" + start + ", " + end);
#endif
                                VsqBPList list = vsq.Track[track].getCurve(mSelectedCurve.getName());
                                long maxid = list.getMaxID();

                                // 削除するものを列挙
                                List<long> delete = new List<long>();
                                int c = list.size();
                                for (int i = 0; i < c; i++) {
                                    int clock = list.getKeyClock(i);
                                    if (clock_start <= clock && clock <= clock_end) {
                                        delete.Add(list.getElementB(i).id);
                                    } else if (clock_end < clock) {
                                        break;
                                    }
                                }

                                SortedDictionary<int, VsqBPPair> add = new SortedDictionary<int, VsqBPPair>();
                                int lvalue = int.MinValue;
                                int lclock = -2 * step_clock;
                                int index = 0;
                                foreach (var p in mMouseTracer.iterator()) {
                                    if (p.X < start) {
                                        continue;
                                    } else if (end < p.X) {
                                        break;
                                    }
                                    int clock = EditorManager.clockFromXCoord(p.X - stdx);
                                    if (clock - lclock < step_clock) {
                                        continue;
                                    }
                                    int value = valueFromYCoord(p.Y);
                                    if (value < min) {
                                        value = min;
                                    } else if (max < value) {
                                        value = max;
                                    }
                                    if (value != lvalue) {
                                        index++;
                                        add[clock] = new VsqBPPair(value, maxid + index);
                                        lvalue = value;
                                        lclock = clock;
                                    }
                                }

                                // clock_endでの値
                                int valueAtEnd = list.getValue(clock_end);
                                if (add.ContainsKey(clock_end)) {
                                    VsqBPPair v = add[clock_end];
                                    v.value = valueAtEnd;
                                    add[clock_end] = v;
                                } else {
                                    index++;
                                    add[clock_end] = new VsqBPPair(valueAtEnd, maxid + index);
                                }

                                CadenciiCommand pen_run = new CadenciiCommand(
                                    VsqCommand.generateCommandTrackCurveEdit2(track, mSelectedCurve.getName(), delete, add));
                                executeCommand(pen_run, true);
                                #endregion
                            }
                        }
                        mMouseTracer.clear();
                        #endregion
                    }
                }
                mMouseDowned = false;
            } else if (mMouseDownMode == MouseDownMode.SINGER_LIST) {
                if (mMouseMoved) {
                    int count = EditorManager.itemSelection.getEventCount();
                    if (count > 0) {
                        int[] ids = new int[count];
                        int[] clocks = new int[count];
                        VsqID[] values = new VsqID[count];
                        int i = -1;
                        bool is_valid = true;
                        bool contains_first_singer = false;
                        int premeasure = vsq.getPreMeasureClocks();
                        foreach (var item in EditorManager.itemSelection.getEventIterator()) {
                            i++;
                            ids[i] = item.original.InternalID;
                            clocks[i] = item.editing.Clock;
                            values[i] = item.original.ID;
                            if (clocks[i] < premeasure) {
                                is_valid = false;
                                // breakしてはいけない。clock=0のを確実に検出するため
                            }
                            if (item.original.Clock == 0) {
                                contains_first_singer = true;
                                break;
                            }
                        }
                        if (contains_first_singer) {
                            System.Media.SystemSounds.Asterisk.Play();
                        } else {
                            if (!is_valid) {
                                int tmin = clocks[0];
                                for (int j = 1; j < count; j++) {
                                    tmin = Math.Min(tmin, clocks[j]);
                                }
                                int dclock = premeasure - tmin;
                                for (int j = 0; j < count; j++) {
                                    clocks[j] += dclock;
                                }
                                System.Media.SystemSounds.Asterisk.Play();
                            }
                            bool changed = false;
                            for (int j = 0; j < ids.Length; j++) {
                                foreach (var item in EditorManager.itemSelection.getEventIterator()) {
                                    if (item.original.InternalID == ids[j] && item.original.Clock != clocks[j]) {
                                        changed = true;
                                        break;
                                    }
                                }
                                if (changed) {
                                    break;
                                }
                            }
                            if (changed) {
                                CadenciiCommand run = new CadenciiCommand(
                                    VsqCommand.generateCommandEventChangeClockAndIDContaintsRange(selected, ids, clocks, values));
                                executeCommand(run, true);
                            }
                        }
                    }
                }
            } else if (mMouseDownMode == MouseDownMode.VEL_EDIT) {
                if (mSelectedCurve.equals(CurveType.VEL) && is_utau_mode) {
                    int count = mVelEditSelected.Count;
                    VsqEvent[] values = new VsqEvent[count];
                    int i = 0;
                    foreach (var internal_id in mVelEditSelected.Keys) {
                        VsqEvent item = (VsqEvent)vsq_track.findEventFromID(internal_id).clone();
                        if (item.UstEvent == null) {
                            item.UstEvent = new UstEvent();
                        }
                        item.UstEvent.setIntensity(mVelEditSelected[internal_id].editing.UstEvent.getIntensity());
                        values[i] = item;
                        i++;
                    }
                    CadenciiCommand run = new CadenciiCommand(
                        VsqCommand.generateCommandEventReplaceRange(selected, values));
                    executeCommand(run, true);
                } else {
                    int count = mVelEditSelected.Count;
                    int[] ids = new int[count];
                    VsqID[] values = new VsqID[count];
                    int i = -1;
                    foreach (var id in mVelEditSelected.Keys) {
                        i++;
                        ids[i] = id;
                        values[i] = (VsqID)mVelEditSelected[id].editing.ID.clone();
                    }
                    CadenciiCommand run = new CadenciiCommand(VsqCommand.generateCommandEventChangeIDContaintsRange(selected, ids, values));
                    executeCommand(run, true);
                }
                if (mVelEditSelected.Count == 1) {
                    EditorManager.itemSelection.clearEvent();
                    EditorManager.itemSelection.addEvent(mVelEditLastSelectedID);
                }
            } else if (mMouseDownMode == MouseDownMode.ENVELOPE_MOVE) {
                mMouseDownMode = MouseDownMode.NONE;
                if (mMouseMoved) {
                    VsqTrack target = vsq_track;
                    VsqEvent edited = (VsqEvent)target.findEventFromID(mEnvelopeEdigintID).clone();

                    // m_envelope_originalに，編集前のが入っているので，いったん置き換え
                    int count = target.getEventCount();
                    for (int i = 0; i < count; i++) {
                        VsqEvent item = target.getEvent(i);
                        if (item.ID.type == VsqIDType.Anote && item.InternalID == mEnvelopeEdigintID) {
                            item.UstEvent.setEnvelope(mEnvelopeOriginal);
                            target.setEvent(i, item);
                            break;
                        }
                    }

                    // コマンドを発行
                    CadenciiCommand run = new CadenciiCommand(VsqCommand.generateCommandEventReplace(selected,
                                                                                                       edited));
                    executeCommand(run, true);
                }
            } else if (mMouseDownMode == MouseDownMode.PRE_UTTERANCE_MOVE) {
                mMouseDownMode = MouseDownMode.NONE;
                if (mMouseMoved) {
                    VsqTrack target = vsq_track;
                    VsqEvent edited = (VsqEvent)target.findEventFromID(mPreUtteranceEditingID).clone();

                    // m_envelope_originalに，編集前のが入っているので，いったん置き換え
                    int count = target.getEventCount();
                    for (int i = 0; i < count; i++) {
                        VsqEvent item = target.getEvent(i);
                        if (item.ID.type == VsqIDType.Anote && item.InternalID == mPreUtteranceEditingID) {
                            target.setEvent(i, mPreOverlapOriginal);
                            break;
                        }
                    }

                    // コマンドを発行
                    CadenciiCommand run = new CadenciiCommand(VsqCommand.generateCommandEventReplace(selected,
                                                                                                       edited));
                    executeCommand(run, true);
                }
            } else if (mMouseDownMode == MouseDownMode.OVERLAP_MOVE) {
                mMouseDownMode = MouseDownMode.NONE;
                if (mMouseMoved) {
                    VsqTrack target = vsq_track;
                    VsqEvent edited = (VsqEvent)target.findEventFromID(mOverlapEditingID).clone();

                    // m_envelope_originalに，編集前のが入っているので，いったん置き換え
                    int count = target.getEventCount();
                    for (int i = 0; i < count; i++) {
                        VsqEvent item = target.getEvent(i);
                        if (item.ID.type == VsqIDType.Anote && item.InternalID == mOverlapEditingID) {
                            target.setEvent(i, mPreOverlapOriginal);
                            break;
                        }
                    }

                    // コマンドを発行
                    CadenciiCommand run = new CadenciiCommand(VsqCommand.generateCommandEventReplace(selected,
                                                                                                       edited));
                    executeCommand(run, true);
                }
            } else if (mMouseDownMode == MouseDownMode.POINT_MOVE) {
                if (mMouseMoved) {
					Point pmouse = pointToClient(Cadencii.Gui.Screen.Instance.GetScreenMousePosition());
                    Point mouse = new Point(pmouse.X, pmouse.Y);
                    int dx = mouse.X + EditorManager.MainWindow.Model.StartToDrawX - mMouseDownLocation.X;
                    int dy = mouse.Y - mMouseDownLocation.Y;

                    string curve = mSelectedCurve.getName();
                    VsqTrack work = (VsqTrack)vsq_track.clone();
                    VsqBPList list = work.getCurve(curve);
                    VsqBPList work_list = (VsqBPList)list.clone();
                    int min0 = list.getMinimum();
                    int max0 = list.getMaximum();
                    int count = list.size();
                    for (int i = 0; i < count; i++) {
                        int clock = list.getKeyClock(i);
                        VsqBPPair item = list.getElementB(i);
                        if (EditorManager.itemSelection.isPointContains(item.id)) {
                            int x = EditorManager.xCoordFromClocks(clock) + dx + 1;
                            int y = yCoordFromValue(item.value) + dy - 1;

                            int nclock = EditorManager.clockFromXCoord(x);
                            int nvalue = valueFromYCoord(y);
                            if (nvalue < min0) {
                                nvalue = min0;
                            }
                            if (max0 < nvalue) {
                                nvalue = max0;
                            }
                            work_list.move(clock, nclock, nvalue);
                        }
                    }
                    work.setCurve(curve, work_list);
                    BezierCurves beziers = vsq.AttachedCurves.get(selected - 1);
                    CadenciiCommand run = VsqFileEx.generateCommandTrackReplace(selected, work, beziers);
                    executeCommand(run, true);
                }
                mMovingPoints.Clear();
            }
            mMouseDownMode = MouseDownMode.NONE;
            Invalidate();
        }

        public void TrackSelector_MouseHover(Object sender, EventArgs e)
        {
#if DEBUG
            CDebug.WriteLine("TrackSelector_MouseHover");
            CDebug.WriteLine("    m_mouse_downed=" + mMouseDowned);
#endif
            //if ( m_selected_curve.equals( CurveType.Accent ) || m_selected_curve.equals( CurveType.Decay ) || m_selected_curve.equals( CurveType.Env ) ) {
            if (mSelectedCurve.equals(CurveType.Env)) {
                return;
            }
            if (mMouseDowned && !mPencilMoved && EditorManager.SelectedTool == EditTool.PENCIL &&
                 !mSelectedCurve.equals(CurveType.VEL)) {
				Point pmouse = pointToClient(Cadencii.Gui.Screen.Instance.GetScreenMousePosition());
                Point mouse = new Point(pmouse.X, pmouse.Y);
                int clock = EditorManager.clockFromXCoord(mouse.X);
                int value = valueFromYCoord(mouse.Y);
                int min = mSelectedCurve.getMinimum();
                int max = mSelectedCurve.getMaximum();

                int selected = EditorManager.Selected;
                VsqFileEx vsq = MusicManager.getVsqFile();
                VsqTrack vsq_track = vsq.Track[selected];

                if (value < min) {
                    value = min;
                } else if (max < value) {
                    value = max;
                }
                if (mSelectedCurve.equals(CurveType.VibratoRate) || mSelectedCurve.equals(CurveType.VibratoDepth)) {
                    // マウスの位置がビブラートの範囲かどうかを調べる
                    float x = -1f;
                    VsqID edited = null;
                    int event_id = -1;
                    foreach (var ve in vsq_track.getNoteEventIterator()) {
                        if (ve.ID.VibratoHandle == null) {
                            continue;
                        }
                        int cl_vib_start = ve.Clock + ve.ID.VibratoDelay;
                        int cl_vib_end = ve.Clock + ve.ID.getLength();
                        if (cl_vib_start <= clock && clock < cl_vib_end) {
                            x = (clock - cl_vib_start) / (float)(cl_vib_end - cl_vib_start);
                            edited = (VsqID)ve.ID.clone();
                            event_id = ve.InternalID;
                            break;
                        }
                    }
#if DEBUG
                    CDebug.WriteLine("    x=" + x);
#endif
                    if (0f <= x && x <= 1f) {
                        if (mSelectedCurve.equals(CurveType.VibratoRate)) {
                            if (x == 0f) {
                                edited.VibratoHandle.setStartRate(value);
                            } else {
                                if (edited.VibratoHandle.getRateBP().getCount() <= 0) {
                                    edited.VibratoHandle.setRateBP(new VibratoBPList(new float[] { x },
                                                                                       new int[] { value }));
                                } else {
                                    List<float> xs = new List<float>();
                                    List<int> vals = new List<int>();
                                    bool first = true;
                                    VibratoBPList ratebp = edited.VibratoHandle.getRateBP();
                                    int c = ratebp.getCount();
                                    for (int i = 0; i < c; i++) {
                                        VibratoBPPair itemi = ratebp.getElement(i);
                                        if (itemi.X < x) {
                                            xs.Add(itemi.X);
                                            vals.Add(itemi.Y);
                                        } else if (itemi.X == x) {
                                            xs.Add(x);
                                            vals.Add(value);
                                            first = false;
                                        } else {
                                            if (first) {
                                                xs.Add(x);
                                                vals.Add(value);
                                                first = false;
                                            }
                                            xs.Add(itemi.X);
                                            vals.Add(itemi.Y);
                                        }
                                    }
                                    if (first) {
                                        xs.Add(x);
                                        vals.Add(value);
                                    }
                                    edited.VibratoHandle.setRateBP(
                                        new VibratoBPList(
                                            PortUtil.convertFloatArray(xs.ToArray()),
                                            PortUtil.convertIntArray(vals.ToArray())));
                                }
                            }
                        } else {
                            if (x == 0f) {
                                edited.VibratoHandle.setStartDepth(value);
                            } else {
                                if (edited.VibratoHandle.getDepthBP().getCount() <= 0) {
                                    edited.VibratoHandle.setDepthBP(
                                        new VibratoBPList(new float[] { x }, new int[] { value }));
                                } else {
                                    List<float> xs = new List<float>();
                                    List<int> vals = new List<int>();
                                    bool first = true;
                                    VibratoBPList depthbp = edited.VibratoHandle.getDepthBP();
                                    int c = depthbp.getCount();
                                    for (int i = 0; i < c; i++) {
                                        VibratoBPPair itemi = depthbp.getElement(i);
                                        if (itemi.X < x) {
                                            xs.Add(itemi.X);
                                            vals.Add(itemi.Y);
                                        } else if (itemi.X == x) {
                                            xs.Add(x);
                                            vals.Add(value);
                                            first = false;
                                        } else {
                                            if (first) {
                                                xs.Add(x);
                                                vals.Add(value);
                                                first = false;
                                            }
                                            xs.Add(itemi.X);
                                            vals.Add(itemi.Y);
                                        }
                                    }
                                    if (first) {
                                        xs.Add(x);
                                        vals.Add(value);
                                    }
                                    edited.VibratoHandle.setDepthBP(
                                        new VibratoBPList(
                                            PortUtil.convertFloatArray(xs.ToArray()),
                                            PortUtil.convertIntArray(vals.ToArray())));
                                }
                            }
                        }
                        CadenciiCommand run = new CadenciiCommand(
                            VsqCommand.generateCommandEventChangeIDContaints(
                                selected,
                                event_id,
                                edited));
                        executeCommand(run, true);
                    }
                } else {
                    VsqBPList list = vsq_track.getCurve(mSelectedCurve.getName());
                    if (list != null) {
                        List<long> delete = new List<long>();
                        SortedDictionary<int, VsqBPPair> add = new SortedDictionary<int, VsqBPPair>();
                        long maxid = list.getMaxID();
                        if (list.isContainsKey(clock)) {
                            int c = list.size();
                            for (int i = 0; i < c; i++) {
                                int cl = list.getKeyClock(i);
                                if (cl == clock) {
                                    delete.Add(list.getElementB(i).id);
                                    break;
                                }
                            }
                        }
                        add[clock] = new VsqBPPair(value, maxid + 1);
                        CadenciiCommand run = new CadenciiCommand(
                            VsqCommand.generateCommandTrackCurveEdit2(selected,
                                                                       mSelectedCurve.getName(),
                                                                       delete,
                                                                       add));
                        executeCommand(run, true);
                    }
                }
            } else if (mMouseDownMode == MouseDownMode.VEL_WAIT_HOVER) {
#if DEBUG
                CDebug.WriteLine("    entered VelEdit");
                CDebug.WriteLine("    m_veledit_selected.Count=" + mVelEditSelected.Count);
                CDebug.WriteLine("    m_veledit_last_selectedid=" + mVelEditLastSelectedID);
                CDebug.WriteLine("    m_veledit_selected.ContainsKey(m_veledit_last_selectedid" + mVelEditSelected.ContainsKey(mVelEditLastSelectedID));
#endif
                mMouseDownMode = MouseDownMode.VEL_EDIT;
                Invalidate();
            }
        }

        private void MouseHoverEventGenerator()
        {
            Thread.Sleep((int)(System.Windows.Forms.SystemInformation.MouseHoverTime * 0.8));
            Invoke(new EventHandler(TrackSelector_MouseHover));
        }

        public void TrackSelector_MouseDoubleClick(Object sender, MouseEventArgs e)
        {
            if (mMouseHoverThread != null && mMouseHoverThread.IsAlive) {
                mMouseHoverThread.Abort();
            }

            VsqFileEx vsq = MusicManager.getVsqFile();
            int selected = EditorManager.Selected;
            VsqTrack vsq_track = vsq.Track[selected];
            int height = getHeight();
            int width = getWidth();
            int key_width = EditorManager.keyWidth;

            if (e.Button == MouseButtons.Left) {
                if (0 <= e.Y && e.Y <= height - 2 * TS.OFFSET_TRACK_TAB) {
                    #region MouseDown occured on curve-pane
                    if (key_width <= e.X && e.X <= width) {
                        if (!mSelectedCurve.equals(CurveType.VEL) &&
                             !mSelectedCurve.equals(CurveType.Accent) &&
                             !mSelectedCurve.equals(CurveType.Decay) &&
                             !mSelectedCurve.equals(CurveType.Env)) {
                            // ベジエデータ点にヒットしているかどうかを検査
                            //int track = EditorManager.Selected;
                            int clock = EditorManager.clockFromXCoord(e.X);
                            List<BezierChain> dict = vsq.AttachedCurves.get(selected - 1).get(mSelectedCurve);
                            BezierChain target_chain = null;
                            BezierPoint target_point = null;
                            bool found = false;
                            int dict_size = dict.Count;
                            for (int i = 0; i < dict_size; i++) {
                                BezierChain bc = dict[i];
                                foreach (var bp in bc.points) {
                                    Point pt = getScreenCoord(bp.getBase());
                                    Rectangle rc = new Rectangle(pt.X - TS.DOT_WID, pt.Y - TS.DOT_WID, 2 * TS.DOT_WID + 1, 2 * TS.DOT_WID + 1);
                                    if (isInRect(e.X, e.Y, rc)) {
                                        found = true;
                                        target_point = (BezierPoint)bp.clone();
                                        target_chain = (BezierChain)bc.clone();
                                        break;
                                    }

                                    if (bp.getControlLeftType() != BezierControlType.None) {
                                        pt = getScreenCoord(bp.getControlLeft());
                                        rc = new Rectangle(pt.X - TS.DOT_WID, pt.Y - TS.DOT_WID, 2 * TS.DOT_WID + 1, 2 * TS.DOT_WID + 1);
                                        if (isInRect(e.X, e.Y, rc)) {
                                            found = true;
                                            target_point = (BezierPoint)bp.clone();
                                            target_chain = (BezierChain)bc.clone();
                                            break;
                                        }
                                    }
                                    if (bp.getControlRightType() != BezierControlType.None) {
                                        pt = getScreenCoord(bp.getControlRight());
                                        rc = new Rectangle(pt.X - TS.DOT_WID, pt.Y - TS.DOT_WID, 2 * TS.DOT_WID + 1, 2 * TS.DOT_WID + 1);
                                        if (isInRect(e.X, e.Y, rc)) {
                                            found = true;
                                            target_point = (BezierPoint)bp.clone();
                                            target_chain = (BezierChain)bc.clone();
                                            break;
                                        }
                                    }
                                }
                                if (found) {
                                    break;
                                }
                            }
                            if (found) {
                                #region ダブルクリックした位置にベジエデータ点があった場合
                                int chain_id = target_chain.id;
                                BezierChain before = (BezierChain)target_chain.clone();
                                FormBezierPointEditController fbpe = null;
                                try {
                                    fbpe = new FormBezierPointEditController(this,
                                                                    mSelectedCurve,
                                                                    chain_id,
                                                                    target_point.getID());
                                    mEditingChainID = chain_id;
                                    mEditingPointID = target_point.getID();
                                    {//TODO:
                                        Logger.StdOut("TrackSelector_MouseDoubleClick; start to show editor");
                                    }
                                    var ret = DialogManager.ShowModalDialog(fbpe.getUi(), mMainWindow);
                                    {//TODO:
                                        Logger.StdOut("TrackSelector_MouseDoubleCLick; ret=" + ret);
                                    }
                                    mEditingChainID = -1;
                                    mEditingPointID = -1;
                                    BezierChain after = vsq.AttachedCurves.get(selected - 1).getBezierChain(mSelectedCurve, chain_id);
                                    // 編集前の状態に戻す
                                    CadenciiCommand revert =
                                        VsqFileEx.generateCommandReplaceBezierChain(
                                            selected,
                                            mSelectedCurve,
                                            chain_id,
                                            before,
                                            ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                                    executeCommand(revert, false);
									if (ret == Cadencii.Gui.DialogResult.OK) {
                                        // ダイアログの結果がOKで、かつベジエ曲線が単調増加なら編集を適用
                                        if (BezierChain.isBezierImplicit(target_chain)) {
                                            CadenciiCommand run =
                                                VsqFileEx.generateCommandReplaceBezierChain(
                                                    selected,
                                                    mSelectedCurve,
                                                    chain_id,
                                                    after,
                                                    ApplicationGlobal.appConfig.getControlCurveResolutionValue());
                                            executeCommand(run, true);
                                        }
                                    }
                                } catch (Exception ex) {
                                } finally {
                                    if (fbpe != null) {
                                        try {
                                            fbpe.getUi().close();
                                        } catch (Exception ex2) {
                                        }
                                    }
                                }
                                #endregion
                            } else {
                                #region ダブルクリックした位置にベジエデータ点が無かった場合
                                VsqBPList list = vsq_track.getCurve(mSelectedCurve.getName());
                                bool bp_found = false;
                                long bp_id = -1;
                                int tclock = 0;
                                if (list != null) {
                                    int list_size = list.size();
                                    for (int i = 0; i < list_size; i++) {
                                        int c = list.getKeyClock(i);
                                        int bpx = EditorManager.xCoordFromClocks(c);
                                        if (e.X < bpx - TS.DOT_WID) {
                                            break;
                                        }
                                        if (bpx - TS.DOT_WID <= e.X && e.X <= bpx + TS.DOT_WID) {
                                            VsqBPPair bp = list.getElementB(i);
                                            int bpy = yCoordFromValue(bp.value);
                                            if (bpy - TS.DOT_WID <= e.Y && e.Y <= bpy + TS.DOT_WID) {
                                                bp_found = true;
                                                bp_id = bp.id;
                                                tclock = c;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (bp_found) {
                                    EditorManager.itemSelection.clearPoint();
                                    EditorManager.itemSelection.addPoint(mSelectedCurve, bp_id);
                                    FormCurvePointEdit dialog = ApplicationUIHost.Create<FormCurvePointEdit> (mMainWindow, bp_id, mSelectedCurve);
                                    int tx = EditorManager.xCoordFromClocks(tclock);
                                    Point pt = pointToScreen(new Point(tx, 0));
                                    Invalidate();
                                    dialog.Location =
                                        new Point(pt.X - dialog.Width / 2, pt.Y - dialog.Height);
                                    DialogManager.ShowModalDialog(dialog, mMainWindow);
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion
                } else if (height - 2 * TS.OFFSET_TRACK_TAB <= e.Y && e.Y <= height - TS.OFFSET_TRACK_TAB) {
                    #region MouseDown occured on singer list
                    if (EditorManager.SelectedTool != EditTool.ERASER) {
                        VsqEvent ve = null;
                        if (key_width <= e.X && e.X <= width) {
                            ve = findItemAt(e.X, e.Y);
                        }
                        RendererKind renderer = VsqFileEx.getTrackRendererKind(vsq_track);
                        if (ve == null) {
                            int x_at_left = key_width + EditorManager.keyOffset;
                            Rectangle rc_left_singer_box =
                                new Rectangle(
                                    x_at_left,
                                    height - 2 * TS.OFFSET_TRACK_TAB + 1,
                                    TS.SINGER_ITEM_WIDTH, TS.OFFSET_TRACK_TAB - 2);
                            if (isInRect(e.X, e.Y, rc_left_singer_box)) {
                                // マウス位置に歌手変更が無かった場合であって、かつ、
                                // マウス位置が左端の常時歌手表示部の内部だった場合
                                int clock_at_left = EditorManager.clockFromXCoord(x_at_left);
                                ve = vsq_track.getSingerEventAt(clock_at_left);
                            }
                        }
                        if (ve != null) {
                            // マウス位置に何らかのアイテムがあった場合
                            if (ve.ID.type != VsqIDType.Singer) {
                                return;
                            }
                            if (!mCMenuSingerPrepared.Equals(renderer)) {
                                prepareSingerMenu(renderer);
                            }
                            cmenuSinger.SingerChangeExists = true;
                            cmenuSinger.InternalID = ve.InternalID;
                            foreach (var item in cmenuSinger.Items) {
                                var tsmi = item as TrackSelectorSingerDropdownMenuItem;
                                if (tsmi.Language == ve.ID.IconHandle.Language &&
                                     tsmi.Program == ve.ID.IconHandle.Program) {
                                    tsmi.Checked = true;
                                } else {
                                    tsmi.Checked = false;
                                }
                            }
                            cmenuSinger.Show(this, e.X, e.Y);
                        } else if (key_width <= e.X && e.X <= width) {
                            // マウス位置に何もアイテムが無かった場合
                            if (!mCMenuSingerPrepared.Equals(renderer)) {
                                prepareSingerMenu(renderer);
                            }
                            string singer = ApplicationGlobal.appConfig.DefaultSingerName;
                            int clock = EditorManager.clockFromXCoord(e.X);
                            int last_clock = 0;
                            for (Iterator<VsqEvent> itr = vsq_track.getSingerEventIterator(); itr.hasNext(); ) {
                                VsqEvent ve2 = itr.next();
                                if (last_clock <= clock && clock < ve2.Clock) {
                                    singer = ((IconHandle)ve2.ID.IconHandle).IDS;
                                    break;
                                }
                                last_clock = ve2.Clock;
                            }
                            cmenuSinger.SingerChangeExists = false;
                            cmenuSinger.Clock = clock;
                            foreach (var item in cmenuSinger.Items) {
                                var tsmi = item as ToolStripMenuItem;
                                tsmi.Checked = false;
                            }
                            cmenuSinger.Show(this, e.X, e.Y);
                        }
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 指定した歌声合成システムの歌手のリストを作成し，コンテキストメニューを準備します．
        /// </summary>
        /// <param name="renderer"></param>
        public void prepareSingerMenu(RendererKind renderer)
        {
            cmenuSinger.Items.Clear();
            List<SingerConfig> items = null;
            if (renderer == RendererKind.UTAU || renderer == RendererKind.VCNT) {
                items = ApplicationGlobal.appConfig.UtauSingers;
            } else if (renderer == RendererKind.VOCALOID1) {
                items = new List<SingerConfig>(VocaloSysUtil.getSingerConfigs(SynthesizerType.VOCALOID1));
            } else if (renderer == RendererKind.VOCALOID2) {
                items = new List<SingerConfig>(VocaloSysUtil.getSingerConfigs(SynthesizerType.VOCALOID2));
#if ENABLE_AQUESTONE
            } else if (renderer == RendererKind.AQUES_TONE) {
                items = new List<SingerConfig>();
                items.AddRange(AquesToneDriver.Singers);
            } else if (renderer == RendererKind.AQUES_TONE2) {
                items = new List<SingerConfig>();
                items.AddRange(AquesTone2Driver.Singers);
#endif
            } else {
                return;
            }
            int count = 0;
            foreach (var sc in items) {
                string tip = "";
                if (renderer == RendererKind.UTAU || renderer == RendererKind.VCNT) {
                    if (sc != null) {
                        tip = "Name: " + sc.VOICENAME +
                              "\nDirectory: " + sc.VOICEIDSTR;
                    }
                } else if (renderer == RendererKind.VOCALOID1) {
                    if (sc != null) {
                        tip = "Original: " + VocaloSysUtil.getOriginalSinger(sc.Language, sc.Program, SynthesizerType.VOCALOID1) +
                              "\nHarmonics: " + sc.Harmonics +
                              "\nNoise: " + sc.Breathiness +
                              "\nBrightness: " + sc.Brightness +
                              "\nClearness: " + sc.Clearness +
                              "\nGender Factor: " + sc.GenderFactor +
                              "\nReso1(Freq,BandWidth,Amp): " + sc.Resonance1Frequency + ", " + sc.Resonance1BandWidth + ", " + sc.Resonance1Amplitude +
                              "\nReso2(Freq,BandWidth,Amp): " + sc.Resonance2Frequency + ", " + sc.Resonance2BandWidth + ", " + sc.Resonance2Amplitude +
                              "\nReso3(Freq,BandWidth,Amp): " + sc.Resonance3Frequency + ", " + sc.Resonance3BandWidth + ", " + sc.Resonance3Amplitude +
                              "\nReso4(Freq,BandWidth,Amp): " + sc.Resonance4Frequency + ", " + sc.Resonance4BandWidth + ", " + sc.Resonance4Amplitude;
                    }
                } else if (renderer == RendererKind.VOCALOID2) {
                    if (sc != null) {
                        tip = "Original: " + VocaloSysUtil.getOriginalSinger(sc.Language, sc.Program, SynthesizerType.VOCALOID2) +
                              "\nBreathiness: " + sc.Breathiness +
                              "\nBrightness: " + sc.Brightness +
                              "\nClearness: " + sc.Clearness +
                              "\nGender Factor: " + sc.GenderFactor +
                              "\nOpening: " + sc.Opening;
                    }
                } else if (renderer == RendererKind.AQUES_TONE || renderer == RendererKind.AQUES_TONE2) {
                    if (sc != null) {
                        tip = "Name: " + sc.VOICENAME;
                    }
                }
                if (sc != null) {
                    TrackSelectorSingerDropdownMenuItem tsmi =
			ApplicationUIHost.Create<TrackSelectorSingerDropdownMenuItem>();
                    tsmi.Text = sc.VOICENAME;
                    tsmi.ToolTipText = tip;
                    tsmi.ToolTipPxWidth = 0;
                    tsmi.Language = sc.Language;
                    tsmi.Program = sc.Program;
                    tsmi.Click += new EventHandler(cmenusinger_Click);
                    tsmi.MouseHover += new EventHandler(cmenusinger_MouseHover);
                    cmenuSinger.Items.Add(tsmi);
                    count++;
                }
            }
            cmenuSinger.VisibleChanged += new EventHandler(cmenuSinger_VisibleChanged);
            mCMenuSingerTooltipWidth = new int[count];
            //m_cmenusinger_map = list.ToArray();
            for (int i = 0; i < count; i++) {
                mCMenuSingerTooltipWidth[i] = 0;
            }
            mCMenuSingerPrepared = renderer;
        }

        public void cmenuSinger_VisibleChanged(Object sender, EventArgs e)
        {
			toolTip.Hide(cmenuSinger);
        }

        public void cmenusinger_MouseEnter(Object sender, EventArgs e)
        {
            cmenusinger_MouseHover(sender, e);
        }

        public void cmenusinger_MouseHover(Object sender, EventArgs e)
        {
            try {
                TrackSelectorSingerDropdownMenuItem menu =
                    (TrackSelectorSingerDropdownMenuItem)sender;
                string tip = menu.ToolTipText;
                int language = menu.Language;
                int program = menu.Program;

                // tooltipを表示するy座標を決める
                int y = 0;
                foreach (var i in cmenuSinger.Items) {
                    var item = i as TrackSelectorSingerDropdownMenuItem;
                    if (language == item.Language &&
                         program == item.Program) {
                        break;
                    }
                    y += item.Height;
                }

                int tip_width = menu.ToolTipPxWidth;
                var pts = cmenuSinger.PointToScreen(new Point(0, 0));
				Rectangle rrc = Cadencii.Gui.Screen.Instance.getScreenBounds(this);
                Rectangle rc = new Rectangle(rrc.X, rrc.Y, rrc.Width, rrc.Height);
                mTooltipProgram = program;
                mTooltipLanguage = language;
                if (pts.X + cmenuSinger.Width + tip_width > rc.Width) {
                    toolTip.Show(tip, cmenuSinger, new Point(-tip_width, y), 5000);
                } else {
                    toolTip.Show(tip, cmenuSinger, new Point(cmenuSinger.Width, y), 5000);
                }
            } catch (Exception ex) {
                Logger.StdOut("TarckSelectro.tsmi_MouseHover; ex=" + ex);
                CDebug.WriteLine("TarckSelectro.tsmi_MouseHover; ex=" + ex);
            }
        }

        public void cmenusinger_Click(Object sender, EventArgs e)
        {
            if (!(sender is TrackSelectorSingerDropdownMenuItem)) {
                return;
            }
            TrackSelectorSingerDropdownMenuItem menu = (TrackSelectorSingerDropdownMenuItem)sender;
            int language = menu.Language;
            int program = menu.Program;
            VsqID item = Utility.getSingerID(mCMenuSingerPrepared, program, language);
            if (item != null) {
                int selected = EditorManager.Selected;
                if (cmenuSinger.SingerChangeExists) {
                    int id = cmenuSinger.InternalID;
                    CadenciiCommand run = new CadenciiCommand(
                        VsqCommand.generateCommandEventChangeIDContaints(selected, id, item));
#if DEBUG
                    CDebug.WriteLine("TrackSelector#tsmi_Click; item.IconHandle.Program" + item.IconHandle.Program);
#endif
                    executeCommand(run, true);
                } else {
                    int clock = cmenuSinger.Clock;
                    VsqEvent ve = new VsqEvent(clock, item);
                    CadenciiCommand run = new CadenciiCommand(VsqCommand.generateCommandEventAdd(selected, ve));
                    executeCommand(run, true);
                }
            }
        }

        private void toolTip_Draw(Object sender, cadencii.DrawToolTipEventArgs e)
        {
            if (!(sender is System.Windows.Forms.ToolTip)) {
                return;
            }

            var rc = e.Bounds;
#if DEBUG
            Logger.StdOut("TrackSelector#toolTip_Draw; sender.GetType()=" + sender.GetType());
#endif

            foreach (var tsi in cmenuSinger.Items) {
                if (!(tsi is TrackSelectorSingerDropdownMenuItem)) {
                    continue;
                }
                TrackSelectorSingerDropdownMenuItem menu =
                    (TrackSelectorSingerDropdownMenuItem)tsi;
                if (menu.Language == mTooltipLanguage &&
                     menu.Program == mTooltipProgram) {
                    menu.ToolTipPxWidth = rc.Width;
                    break;
                }
            }
            e.DrawBackground();
            e.DrawBorder();
            e.DrawText(TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoFullWidthCharacterBreak);
        }

        public void TrackSelector_KeyDown(Object sender, KeyEventArgs e)
        {
            if (((Keys) e.KeyCode & Keys.Space) == Keys.Space) {
                mSpaceKeyDowned = true;
            }
        }

        public void TrackSelector_KeyUp(Object sender, KeyEventArgs e)
        {
            if (((Keys) e.KeyCode & Keys.Space) == Keys.Space) {
                mSpaceKeyDowned = false;
            }
        }

        public void cmenuCurveCommon_Click(Object sender, EventArgs e)
        {
            if (sender is UiToolStripMenuItem) {
                var tsmi = (UiToolStripMenuItem)sender;
                CurveType curve = getCurveTypeFromMenu(tsmi);
                if (!curve.Equals(CurveType.Empty)) {
                    changeCurve(curve);
                }
            }
        }

        private void registerEventHandlers()
        {
			this.toolTip.Draw += new EventHandler<DrawToolTipEventArgs>(this.toolTip_Draw);
            this.cmenuCurveVelocity.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveAccent.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveDecay.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveDynamics.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveVibratoRate.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveVibratoDepth.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso1Freq.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso1BW.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso1Amp.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso2Freq.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso2BW.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso2Amp.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso3Freq.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso3BW.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso3Amp.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso4Freq.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso4BW.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveReso4Amp.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveHarmonics.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveBreathiness.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveBrightness.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveClearness.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveOpening.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveGenderFactor.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurvePortamentoTiming.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurvePitchBend.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurvePitchBendSensitivity.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveEffect2Depth.Click += new EventHandler(cmenuCurveCommon_Click);
            this.cmenuCurveEnvelope.Click += new EventHandler(cmenuCurveCommon_Click);
            this.Load += new EventHandler(this.TrackSelector_Load);

            this.MouseMove += new MouseEventHandler(TrackSelector_MouseMove);
            this.MouseDoubleClick += new MouseEventHandler(TrackSelector_MouseDoubleClick);
            this.KeyUp += new KeyEventHandler(TrackSelector_KeyUp);
            this.MouseClick += new MouseEventHandler(TrackSelector_MouseClick);
            this.MouseDown += new MouseEventHandler(onMouseDown);
            this.MouseUp += new MouseEventHandler(onMouseUp);
            this.KeyDown += new KeyEventHandler(TrackSelector_KeyDown);
        }

        private void setResources()
        {
        }

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

        #region コンポーネント デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
			this.cmenuSinger = ApplicationUIHost.Create<TrackSelectorSingerPopupMenu>(this.components);
            this.toolTip = new ToolTipImpl(this.components);
            this.cmenuCurve = new ContextMenuStripImpl(this.components);
            this.cmenuCurveVelocity = new ToolStripMenuItemImpl();
            this.cmenuCurveAccent = new ToolStripMenuItemImpl();
            this.cmenuCurveDecay = new ToolStripMenuItemImpl();
            this.cmenuCurveSeparator1 = new ToolStripSeparatorImpl();
            this.cmenuCurveDynamics = new ToolStripMenuItemImpl();
            this.cmenuCurveVibratoRate = new ToolStripMenuItemImpl();
            this.cmenuCurveVibratoDepth = new ToolStripMenuItemImpl();
            this.cmenuCurveSeparator2 = new ToolStripSeparatorImpl();
            this.cmenuCurveReso1 = new ToolStripMenuItemImpl();
            this.cmenuCurveReso1Freq = new ToolStripMenuItemImpl();
            this.cmenuCurveReso1BW = new ToolStripMenuItemImpl();
            this.cmenuCurveReso1Amp = new ToolStripMenuItemImpl();
            this.cmenuCurveReso2 = new ToolStripMenuItemImpl();
            this.cmenuCurveReso2Freq = new ToolStripMenuItemImpl();
            this.cmenuCurveReso2BW = new ToolStripMenuItemImpl();
            this.cmenuCurveReso2Amp = new ToolStripMenuItemImpl();
            this.cmenuCurveReso3 = new ToolStripMenuItemImpl();
            this.cmenuCurveReso3Freq = new ToolStripMenuItemImpl();
            this.cmenuCurveReso3BW = new ToolStripMenuItemImpl();
            this.cmenuCurveReso3Amp = new ToolStripMenuItemImpl();
            this.cmenuCurveReso4 = new ToolStripMenuItemImpl();
            this.cmenuCurveReso4Freq = new ToolStripMenuItemImpl();
            this.cmenuCurveReso4BW = new ToolStripMenuItemImpl();
            this.cmenuCurveReso4Amp = new ToolStripMenuItemImpl();
            this.cmenuCurveSeparator3 = new ToolStripSeparatorImpl();
            this.cmenuCurveHarmonics = new ToolStripMenuItemImpl();
            this.cmenuCurveBreathiness = new ToolStripMenuItemImpl();
            this.cmenuCurveBrightness = new ToolStripMenuItemImpl();
            this.cmenuCurveClearness = new ToolStripMenuItemImpl();
            this.cmenuCurveOpening = new ToolStripMenuItemImpl();
            this.cmenuCurveGenderFactor = new ToolStripMenuItemImpl();
            this.cmenuCurveSeparator4 = new ToolStripSeparatorImpl();
            this.cmenuCurvePortamentoTiming = new ToolStripMenuItemImpl();
            this.cmenuCurvePitchBend = new ToolStripMenuItemImpl();
            this.cmenuCurvePitchBendSensitivity = new ToolStripMenuItemImpl();
            this.cmenuCurveSeparator5 = new ToolStripSeparatorImpl();
            this.cmenuCurveEffect2Depth = new ToolStripMenuItemImpl();
            this.cmenuCurveEnvelope = new ToolStripMenuItemImpl();
            this.cmenuCurve.SuspendLayout();
            this.SuspendLayout();
            //
            // cmenuSinger
            //
            this.cmenuSinger.Name = "cmenuSinger";
            this.cmenuSinger.RenderMode = ToolStripRenderMode.System;
            this.cmenuSinger.ShowCheckMargin = true;
            this.cmenuSinger.ShowImageMargin = false;
			this.cmenuSinger.Size =new Dimension(153, 26);
            //
            // toolTip
            //
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 500;
            this.toolTip.OwnerDraw = true;
            this.toolTip.ReshowDelay = 0;
            //
            // cmenuCurve
            //
            this.cmenuCurve.Items.AddRange(new UiToolStripItem[] {
            this.cmenuCurveVelocity,
            this.cmenuCurveAccent,
            this.cmenuCurveDecay,
            this.cmenuCurveSeparator1,
            this.cmenuCurveDynamics,
            this.cmenuCurveVibratoRate,
            this.cmenuCurveVibratoDepth,
            this.cmenuCurveSeparator2,
            this.cmenuCurveReso1,
            this.cmenuCurveReso2,
            this.cmenuCurveReso3,
            this.cmenuCurveReso4,
            this.cmenuCurveSeparator3,
            this.cmenuCurveHarmonics,
            this.cmenuCurveBreathiness,
            this.cmenuCurveBrightness,
            this.cmenuCurveClearness,
            this.cmenuCurveOpening,
            this.cmenuCurveGenderFactor,
            this.cmenuCurveSeparator4,
            this.cmenuCurvePortamentoTiming,
            this.cmenuCurvePitchBend,
            this.cmenuCurvePitchBendSensitivity,
            this.cmenuCurveSeparator5,
            this.cmenuCurveEffect2Depth,
            this.cmenuCurveEnvelope});
            this.cmenuCurve.Name = "cmenuCurve";
			this.cmenuCurve.RenderMode = ToolStripRenderMode.System;
            this.cmenuCurve.ShowImageMargin = false;
			this.cmenuCurve.Size = new Dimension(160, 496);
            //
            // cmenuCurveVelocity
            //
            this.cmenuCurveVelocity.Name = "cmenuCurveVelocity";
            this.cmenuCurveVelocity.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveVelocity.Text = "Velocity(&V)";
            //
            // cmenuCurveAccent
            //
            this.cmenuCurveAccent.Name = "cmenuCurveAccent";
            this.cmenuCurveAccent.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveAccent.Text = "Accent";
            //
            // cmenuCurveDecay
            //
            this.cmenuCurveDecay.Name = "cmenuCurveDecay";
            this.cmenuCurveDecay.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveDecay.Text = "Decay";
            //
            // cmenuCurveSeparator1
            //
            this.cmenuCurveSeparator1.Name = "cmenuCurveSeparator1";
            this.cmenuCurveSeparator1.Size =new Cadencii.Gui.Dimension(156, 6);
            //
            // cmenuCurveDynamics
            //
            this.cmenuCurveDynamics.Name = "cmenuCurveDynamics";
            this.cmenuCurveDynamics.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveDynamics.Text = "Dynamics";
            //
            // cmenuCurveVibratoRate
            //
            this.cmenuCurveVibratoRate.Name = "cmenuCurveVibratoRate";
            this.cmenuCurveVibratoRate.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveVibratoRate.Text = "Vibrato Rate";
            //
            // cmenuCurveVibratoDepth
            //
            this.cmenuCurveVibratoDepth.Name = "cmenuCurveVibratoDepth";
            this.cmenuCurveVibratoDepth.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveVibratoDepth.Text = "Vibrato Depth";
            //
            // cmenuCurveSeparator2
            //
            this.cmenuCurveSeparator2.Name = "cmenuCurveSeparator2";
            this.cmenuCurveSeparator2.Size =new Cadencii.Gui.Dimension(156, 6);
            //
            // cmenuCurveReso1
            //
            this.cmenuCurveReso1.DropDownItems.AddRange(new UiToolStripItem[] {
            this.cmenuCurveReso1Freq,
            this.cmenuCurveReso1BW,
            this.cmenuCurveReso1Amp});
            this.cmenuCurveReso1.Name = "cmenuCurveReso1";
            this.cmenuCurveReso1.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveReso1.Text = "Resonance 1";
            //
            // cmenuCurveReso1Freq
            //
            this.cmenuCurveReso1Freq.Name = "cmenuCurveReso1Freq";
            this.cmenuCurveReso1Freq.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso1Freq.Text = "Frequency";
            //
            // cmenuCurveReso1BW
            //
            this.cmenuCurveReso1BW.Name = "cmenuCurveReso1BW";
            this.cmenuCurveReso1BW.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso1BW.Text = "Band Width";
            //
            // cmenuCurveReso1Amp
            //
            this.cmenuCurveReso1Amp.Name = "cmenuCurveReso1Amp";
            this.cmenuCurveReso1Amp.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso1Amp.Text = "Amplitude";
            //
            // cmenuCurveReso2
            //
            this.cmenuCurveReso2.DropDownItems.AddRange(new UiToolStripItem[] {
            this.cmenuCurveReso2Freq,
            this.cmenuCurveReso2BW,
            this.cmenuCurveReso2Amp});
            this.cmenuCurveReso2.Name = "cmenuCurveReso2";
            this.cmenuCurveReso2.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveReso2.Text = "Resonance 2";
            //
            // cmenuCurveReso2Freq
            //
            this.cmenuCurveReso2Freq.Name = "cmenuCurveReso2Freq";
            this.cmenuCurveReso2Freq.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso2Freq.Text = "Frequency";
            //
            // cmenuCurveReso2BW
            //
            this.cmenuCurveReso2BW.Name = "cmenuCurveReso2BW";
            this.cmenuCurveReso2BW.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso2BW.Text = "Band Width";
            //
            // cmenuCurveReso2Amp
            //
            this.cmenuCurveReso2Amp.Name = "cmenuCurveReso2Amp";
            this.cmenuCurveReso2Amp.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso2Amp.Text = "Amplitude";
            //
            // cmenuCurveReso3
            //
            this.cmenuCurveReso3.DropDownItems.AddRange(new UiToolStripItem[] {
            this.cmenuCurveReso3Freq,
            this.cmenuCurveReso3BW,
            this.cmenuCurveReso3Amp});
            this.cmenuCurveReso3.Name = "cmenuCurveReso3";
            this.cmenuCurveReso3.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveReso3.Text = "Resonance 3";
            //
            // cmenuCurveReso3Freq
            //
            this.cmenuCurveReso3Freq.Name = "cmenuCurveReso3Freq";
            this.cmenuCurveReso3Freq.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso3Freq.Text = "Frequency";
            //
            // cmenuCurveReso3BW
            //
            this.cmenuCurveReso3BW.Name = "cmenuCurveReso3BW";
            this.cmenuCurveReso3BW.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso3BW.Text = "Band Width";
            //
            // cmenuCurveReso3Amp
            //
            this.cmenuCurveReso3Amp.Name = "cmenuCurveReso3Amp";
            this.cmenuCurveReso3Amp.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso3Amp.Text = "Amplitude";
            //
            // cmenuCurveReso4
            //
            this.cmenuCurveReso4.DropDownItems.AddRange(new UiToolStripItem[] {
            this.cmenuCurveReso4Freq,
            this.cmenuCurveReso4BW,
            this.cmenuCurveReso4Amp});
            this.cmenuCurveReso4.Name = "cmenuCurveReso4";
            this.cmenuCurveReso4.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveReso4.Text = "Resonance 4";
            //
            // cmenuCurveReso4Freq
            //
            this.cmenuCurveReso4Freq.Name = "cmenuCurveReso4Freq";
            this.cmenuCurveReso4Freq.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso4Freq.Text = "Frequency";
            //
            // cmenuCurveReso4BW
            //
            this.cmenuCurveReso4BW.Name = "cmenuCurveReso4BW";
            this.cmenuCurveReso4BW.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso4BW.Text = "Band Width";
            //
            // cmenuCurveReso4Amp
            //
            this.cmenuCurveReso4Amp.Name = "cmenuCurveReso4Amp";
            this.cmenuCurveReso4Amp.Size =new Cadencii.Gui.Dimension(128, 22);
            this.cmenuCurveReso4Amp.Text = "Amplitude";
            //
            // cmenuCurveSeparator3
            //
            this.cmenuCurveSeparator3.Name = "cmenuCurveSeparator3";
            this.cmenuCurveSeparator3.Size =new Cadencii.Gui.Dimension(156, 6);
            //
            // cmenuCurveHarmonics
            //
            this.cmenuCurveHarmonics.Name = "cmenuCurveHarmonics";
            this.cmenuCurveHarmonics.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveHarmonics.Text = "Harmonics";
            //
            // cmenuCurveBreathiness
            //
            this.cmenuCurveBreathiness.Name = "cmenuCurveBreathiness";
            this.cmenuCurveBreathiness.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveBreathiness.Text = "Noise";
            //
            // cmenuCurveBrightness
            //
            this.cmenuCurveBrightness.Name = "cmenuCurveBrightness";
            this.cmenuCurveBrightness.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveBrightness.Text = "Brightness";
            //
            // cmenuCurveClearness
            //
            this.cmenuCurveClearness.Name = "cmenuCurveClearness";
            this.cmenuCurveClearness.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveClearness.Text = "Clearness";
            //
            // cmenuCurveOpening
            //
            this.cmenuCurveOpening.Name = "cmenuCurveOpening";
            this.cmenuCurveOpening.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveOpening.Text = "Opening";
            //
            // cmenuCurveGenderFactor
            //
            this.cmenuCurveGenderFactor.Name = "cmenuCurveGenderFactor";
            this.cmenuCurveGenderFactor.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveGenderFactor.Text = "Gender Factor";
            //
            // cmenuCurveSeparator4
            //
            this.cmenuCurveSeparator4.Name = "cmenuCurveSeparator4";
            this.cmenuCurveSeparator4.Size =new Cadencii.Gui.Dimension(156, 6);
            //
            // cmenuCurvePortamentoTiming
            //
            this.cmenuCurvePortamentoTiming.Name = "cmenuCurvePortamentoTiming";
            this.cmenuCurvePortamentoTiming.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurvePortamentoTiming.Text = "Portamento Timing";
            //
            // cmenuCurvePitchBend
            //
            this.cmenuCurvePitchBend.Name = "cmenuCurvePitchBend";
            this.cmenuCurvePitchBend.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurvePitchBend.Text = "Pitch Bend";
            //
            // cmenuCurvePitchBendSensitivity
            //
            this.cmenuCurvePitchBendSensitivity.Name = "cmenuCurvePitchBendSensitivity";
            this.cmenuCurvePitchBendSensitivity.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurvePitchBendSensitivity.Text = "Pitch Bend Sensitivity";
            //
            // cmenuCurveSeparator5
            //
            this.cmenuCurveSeparator5.Name = "cmenuCurveSeparator5";
            this.cmenuCurveSeparator5.Size =new Cadencii.Gui.Dimension(156, 6);
            //
            // cmenuCurveEffect2Depth
            //
            this.cmenuCurveEffect2Depth.Name = "cmenuCurveEffect2Depth";
            this.cmenuCurveEffect2Depth.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveEffect2Depth.Text = "Effect2 Depth";
            //
            // cmenuCurveEnvelope
            //
            this.cmenuCurveEnvelope.Name = "cmenuCurveEnvelope";
            this.cmenuCurveEnvelope.Size =new Cadencii.Gui.Dimension(159, 22);
            this.cmenuCurveEnvelope.Text = "Envelope";
            //
            // TrackSelector
            //
			this.AutoScaleDimensions =new  System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkGray;
            this.DoubleBuffered = true;
            this.Name = "TrackSelector";
			this.Size =new System.Drawing.Size(430, 228);
            this.cmenuCurve.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private TrackSelectorSingerPopupMenu cmenuSinger;
        private UiToolTip toolTip;
        private UiContextMenuStrip cmenuCurve;
        private UiToolStripMenuItem cmenuCurveVelocity;
        private UiToolStripSeparator cmenuCurveSeparator2;
        private UiToolStripMenuItem cmenuCurveReso1;
        private UiToolStripMenuItem cmenuCurveReso1Freq;
        private UiToolStripMenuItem cmenuCurveReso1BW;
        private UiToolStripMenuItem cmenuCurveReso1Amp;
        private UiToolStripMenuItem cmenuCurveReso2;
        private UiToolStripMenuItem cmenuCurveReso2Freq;
        private UiToolStripMenuItem cmenuCurveReso2BW;
        private UiToolStripMenuItem cmenuCurveReso2Amp;
        private UiToolStripMenuItem cmenuCurveReso3;
        private UiToolStripMenuItem cmenuCurveReso3Freq;
        private UiToolStripMenuItem cmenuCurveReso3BW;
        private UiToolStripMenuItem cmenuCurveReso3Amp;
        private UiToolStripMenuItem cmenuCurveReso4;
        private UiToolStripMenuItem cmenuCurveReso4Freq;
        private UiToolStripMenuItem cmenuCurveReso4BW;
        private UiToolStripMenuItem cmenuCurveReso4Amp;
        private UiToolStripSeparator cmenuCurveSeparator3;
        private UiToolStripMenuItem cmenuCurveHarmonics;
        private UiToolStripMenuItem cmenuCurveDynamics;
        private UiToolStripSeparator cmenuCurveSeparator1;
        private UiToolStripMenuItem cmenuCurveBreathiness;
        private UiToolStripMenuItem cmenuCurveBrightness;
        private UiToolStripMenuItem cmenuCurveClearness;
        private UiToolStripMenuItem cmenuCurveGenderFactor;
        private UiToolStripSeparator cmenuCurveSeparator4;
        private UiToolStripMenuItem cmenuCurvePortamentoTiming;
        private UiToolStripSeparator cmenuCurveSeparator5;
        private UiToolStripMenuItem cmenuCurveEffect2Depth;
        private UiToolStripMenuItem cmenuCurveOpening;
        private UiToolStripMenuItem cmenuCurveAccent;
        private UiToolStripMenuItem cmenuCurveDecay;
        private UiToolStripMenuItem cmenuCurveVibratoRate;
        private UiToolStripMenuItem cmenuCurveVibratoDepth;
        private UiToolStripMenuItem cmenuCurvePitchBend;
        private UiToolStripMenuItem cmenuCurvePitchBendSensitivity;
        private UiToolStripMenuItem cmenuCurveEnvelope;

        #endregion
    }

}
