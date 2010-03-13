﻿/*
 * VocaloSysUtil.s
 * Copyright (C) 2009-2010 kbinani
 *
 * This file is part of org.kbinani.vsq.
 *
 * org.kbinani.vsq is free software; you can redistribute it and/or
 * modify it under the terms of the BSD License.
 *
 * org.kbinani.vsq is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 */
#if JAVA
package org.kbinani.vsq;

import java.io.*;
import java.util.*;
import org.kbinani.*;
#else
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using org.kbinani;
using org.kbinani.java.util;
using org.kbinani.java.io;

namespace org.kbinani.vsq {
    using boolean = System.Boolean;
#endif

    /// <summary>
    /// VOCALOID / VOCALOID2システムについての情報を取得するユーティリティ。
    /// </summary>
    public class VocaloSysUtil {
        private static TreeMap<SynthesizerType, SingerConfigSys> s_singer_config_sys = new TreeMap<SynthesizerType, SingerConfigSys>();
        private static TreeMap<SynthesizerType, ExpressionConfigSys> s_exp_config_sys = new TreeMap<SynthesizerType, ExpressionConfigSys>();
        private static TreeMap<SynthesizerType, String> s_path_vsti = new TreeMap<SynthesizerType, String>();
        private static TreeMap<SynthesizerType, String> s_path_editor = new TreeMap<SynthesizerType, String>();
        private static Boolean isInitialized = false;
        /// <summary>
        /// VOCALOID1の、デフォルトのSynthesize Engineバージョン。1.0の場合100, 1.1の場合101。規定では100(1.0)。
        /// initメソッドにおいて、VOCALOID.iniから読み取る
        /// </summary>
        private static int defaultDseVersion = 100;
        /// <summary>
        /// VOCALOID1にて、バージョン1.1のSynthesize Engineが利用可能かどうか。
        /// 既定ではfalse。DSE1_1.dllが存在するかどうかで判定。
        /// </summary>
        private static boolean dseVersion101Available = false;

        private VocaloSysUtil() {
        }

        /// <summary>
        /// VOCALOID1にて、バージョン1.1のSynthesize Engineが利用可能かどうか。
        /// 既定ではfalse。DSE1_1.dllが存在するかどうかで判定。
        /// </summary>
        public static boolean isDSEVersion101Available() {
            return dseVersion101Available;
        }

        /// <summary>
        /// VOCALOID1の、デフォルトのSynthesize Engineバージョンを取得します。
        /// 1.0の場合100, 1.1の場合101。規定では100(1.0)。
        /// </summary>
        public static int getDefaultDseVersion() {
            if ( !isInitialized ) {
                init();
            }
            return defaultDseVersion;
        }

        /// <summary>
        /// インストールされているVOCALOID / VOCALOID2についての情報を読み込み、初期化します。
        /// </summary>
        public static void init() {
            if ( isInitialized ) {
                return;
            }
            ExpressionConfigSys exp_config_sys1 = null;
            try {
                Vector<String> dir1 = new Vector<String>();
                ByRef<String> path_voicedb1 = new ByRef<String>( "" );
                ByRef<String> path_expdb1 = new ByRef<String>( "" );
                Vector<String> installed_singers1 = new Vector<String>();
                String header1 = "HKLM\\SOFTWARE\\VOCALOID";
                initPrint( "SOFTWARE\\VOCALOID", header1, dir1 );
#if DEBUG
                BufferedWriter sw = null;
                try {
                    sw = new BufferedWriter( new FileWriter( PortUtil.combinePath( System.Windows.Forms.Application.StartupPath, "reg_keys_vocalo1.txt" ) ) );
                    foreach ( String s in dir1 ) {
                        sw.write( s );
                        sw.newLine();
                    }
                } catch ( Exception ex ) {
                    PortUtil.stderr.println( "VocaloSysUtil#init; ex=" + ex );
                } finally {
                    if ( sw != null ) {
                        try {
                            sw.close();
                        } catch ( Exception ex2 ) {
                        }
                    }
                }
#endif
                ByRef<String> path_vsti = new ByRef<String>( "" );
                ByRef<String> path_editor = new ByRef<String>( "" );
                initExtract( dir1,
                         header1,
                         path_vsti,
                         path_voicedb1,
                         path_expdb1,
                         path_editor,
                         installed_singers1 );
                s_path_vsti.put( SynthesizerType.VOCALOID1, path_vsti.value );
                s_path_editor.put( SynthesizerType.VOCALOID1, path_editor.value );
                SingerConfigSys singer_config_sys = new SingerConfigSys( path_voicedb1.value, installed_singers1.toArray( new String[] { } ) );
                String expression_map = PortUtil.combinePath( path_expdb1.value, "expression.map" );
                if ( PortUtil.isFileExists( expression_map ) ) {
                    exp_config_sys1 = new ExpressionConfigSys( path_editor.value, path_expdb1.value );
                }
                s_singer_config_sys.put( SynthesizerType.VOCALOID1, singer_config_sys );

                // DSE1_1.dllがあるかどうか？
                if ( !path_vsti.value.Equals( "" ) ) {
                    String path_dll = PortUtil.getDirectoryName( path_vsti.value );
                    String dse1_1 = PortUtil.combinePath( path_dll, "DSE1_1.dll" );
                    dseVersion101Available = PortUtil.isFileExists( dse1_1 );
                } else {
                    dseVersion101Available = false;
                }

                // VOCALOID.iniから、DSEVersionを取得
                if ( path_editor.value != null && !path_editor.value.Equals( "" ) && PortUtil.isFileExists( path_editor.value ) ) {
                    String dir = PortUtil.getDirectoryName( path_editor.value );
                    String ini = PortUtil.combinePath( dir, "VOCALOID.ini" );
                    if ( PortUtil.isFileExists( ini ) ) {
                        BufferedReader br = null;
                        try {
                            br = new BufferedReader( new InputStreamReader( new FileInputStream( ini ), "Shift_JIS" ) );
                            while ( br.ready() ) {
                                String line = br.readLine();
                                if ( line == null ) continue;
                                if ( line.Equals( "" ) ) continue;
                                if ( line.StartsWith( "DSEVersion" ) ) {
                                    String[] spl = PortUtil.splitString( line, '=' );
                                    if ( spl.Length >= 2 ) {
                                        String str_dse_version = spl[1];
                                        try {
                                            defaultDseVersion = PortUtil.parseInt( str_dse_version );
                                        } catch ( Exception ex ) {
                                            PortUtil.stderr.println( "VocaloSysUtil#init; ex=" + ex );
                                            defaultDseVersion = 100;
                                        }
                                    }
                                    break;
                                }
                            }
                        } catch ( Exception ex ) {
                            PortUtil.stderr.println( "VocaloSysUtil#init; ex=" + ex );
                        } finally {
                            if ( br != null ) {
                                try {
                                    br.close();
                                } catch ( Exception ex2 ) {
                                    PortUtil.stderr.println( "VocaloSysUtil#init; ex2=" + ex2 );
                                }
                            }
                        }
                    }
                }
            } catch ( Exception ex ) {
                PortUtil.stderr.println( "VocaloSysUtil#init; ex=" + ex );
                SingerConfigSys singer_config_sys = new SingerConfigSys( "", new String[] { } );
                exp_config_sys1 = null;
                s_singer_config_sys.put( SynthesizerType.VOCALOID1, singer_config_sys );
            }
            if ( exp_config_sys1 == null ) {
                exp_config_sys1 = ExpressionConfigSys.getVocaloid1Default();
            }
            s_exp_config_sys.put( SynthesizerType.VOCALOID1, exp_config_sys1 );

            ExpressionConfigSys exp_config_sys2 = null;
            try {
                Vector<String> dir2 = new Vector<String>();
                ByRef<String> path_voicedb2 = new ByRef<String>( "" );
                ByRef<String> path_expdb2 = new ByRef<String>( "" );
                Vector<String> installed_singers2 = new Vector<String>();
                String header2 = "HKLM\\SOFTWARE\\VOCALOID2";
                initPrint( "SOFTWARE\\VOCALOID2", header2, dir2 );
#if DEBUG
                BufferedWriter sw = null;
                try {
                    sw = new BufferedWriter( new FileWriter( PortUtil.combinePath( System.Windows.Forms.Application.StartupPath, "reg_keys_vocalo2.txt" ) ) );
                    foreach ( String s in dir2 ) {
                        sw.write( s );
                        sw.newLine();
                    }
                } catch ( Exception ex ) {
                    PortUtil.stderr.println( "VocaloSysUtil#.cctor; ex=" + ex );
                } finally {
                    if ( sw != null ) {
                        try {
                            sw.close();
                        } catch ( Exception ex2 ) {
                            PortUtil.stderr.println( "VocaloSysUtil#.cctor; ex2=" + ex2 );
                        }
                    }
                }
#endif
                ByRef<String> path_vsti = new ByRef<String>( "" );
                ByRef<String> path_editor = new ByRef<String>( "" );
                initExtract( dir2,
                         header2,
                         path_vsti,
                         path_voicedb2,
                         path_expdb2,
                         path_editor,
                         installed_singers2 );
                s_path_vsti.put( SynthesizerType.VOCALOID2, path_vsti.value );
                s_path_editor.put( SynthesizerType.VOCALOID2, path_editor.value );
                SingerConfigSys singer_config_sys = new SingerConfigSys( path_voicedb2.value, installed_singers2.toArray( new String[] { } ) );
                if ( PortUtil.isFileExists( PortUtil.combinePath( path_expdb2.value, "expression.map" ) ) ) {
                    exp_config_sys2 = new ExpressionConfigSys( path_editor.value, path_expdb2.value );
                }
                s_singer_config_sys.put( SynthesizerType.VOCALOID2, singer_config_sys );
            } catch ( Exception ex ) {
                PortUtil.stderr.println( "VocaloSysUtil..cctor; ex=" + ex );
                SingerConfigSys singer_config_sys = new SingerConfigSys( "", new String[] { } );
                exp_config_sys2 = null;
                s_singer_config_sys.put( SynthesizerType.VOCALOID2, singer_config_sys );
            }
            if ( exp_config_sys2 == null ) {
#if DEBUG
                PortUtil.println( "VocaloSysUtil#.ctor; loading default ExpressionConfigSys..." );
#endif
                exp_config_sys2 = ExpressionConfigSys.getVocaloid2Default();
            }
            s_exp_config_sys.put( SynthesizerType.VOCALOID2, exp_config_sys2 );

            isInitialized = true;
        }

        /// <summary>
        /// ビブラートのプリセットタイプから，VibratoHandleを作成します
        /// </summary>
        /// <param name="type"></param>
        /// <param name="vibrato_clocks"></param>
        /// <returns></returns>
        public static VibratoHandle getDefaultVibratoHandle( String icon_id, int vibrato_length, SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            if ( s_exp_config_sys.containsKey( type ) ) {
                for ( Iterator itr = s_exp_config_sys.get( type ).vibratoConfigIterator(); itr.hasNext(); ) {
                    VibratoHandle vconfig = (VibratoHandle)itr.next();
                    if ( vconfig.IconID.Equals( icon_id ) ) {
                        VibratoHandle ret = (VibratoHandle)vconfig.clone();
                        ret.setLength( vibrato_length );
                        return ret;
                    }
                }
            }
            VibratoHandle empty = new VibratoHandle();
            empty.IconID = "$04040000";
            return empty;
        }

        private static void initExtract( Vector<String> dir,
                                     String header,
                                     ByRef<String> path_vsti,
                                     ByRef<String> path_voicedb,
                                     ByRef<String> path_expdb,
                                     ByRef<String> path_editor,
                                     Vector<String> installed_singers ) {
            Vector<String> application = new Vector<String>();
            Vector<String> expression = new Vector<String>();
            Vector<String> voice = new Vector<String>();
            path_vsti.value = "";
            path_expdb.value = "";
            path_voicedb.value = "";
            path_editor.value = "";
            for ( Iterator itr = dir.iterator(); itr.hasNext(); ) {
                String s = (String)itr.next();
                if ( s.StartsWith( header + "\\APPLICATION" ) ) {
                    application.add( s.Substring( PortUtil.getStringLength( header + "\\APPLICATION" ) ) );
                } else if ( s.StartsWith( header + "\\DATABASE\\EXPRESSION" ) ) {
                    expression.add( s.Substring( PortUtil.getStringLength( header + "\\DATABASE\\EXPRESSION" ) ) );
                } else if ( s.StartsWith( header + "\\DATABASE\\VOICE" ) ) {
                    voice.add( s.Substring( PortUtil.getStringLength( header + "\\DATABASE\\VOICE\\" ) ) );
                }
            }

            // path_vstiを取得
            for ( Iterator itr = application.iterator(); itr.hasNext(); ) {
                String s = (String)itr.next();
                String[] spl = PortUtil.splitString( s, '\t' );
                if ( spl.Length >= 3 && spl[1].Equals( "PATH" ) ) {
                    if ( spl[2].ToLower().EndsWith( ".dll" ) ) {
                        path_vsti.value = spl[2];
                    } else if ( spl[2].ToLower().EndsWith( ".exe" ) ) {
                        path_editor.value = spl[2];
                    }
                }
            }

            // path_vicedbを取得
            TreeMap<String, String> install_dirs = new TreeMap<String, String>();
            // 最初はpath_voicedbの取得と、id（BHXXXXXXXXXXXXXXXX）のようなシリアルを取得
            for ( Iterator itr = voice.iterator(); itr.hasNext(); ) {
                String s = (String)itr.next();
                String[] spl = PortUtil.splitString( s, '\t' );
                if ( spl.Length >= 2 ) {
                    if ( spl[0].Equals( "VOICEDIR" ) ) {
                        path_voicedb.value = spl[1];
                    } else if ( spl.Length >= 3 ) {
                        String[] spl2 = PortUtil.splitString( spl[0], '\\' );
                        if ( spl2.Length == 1 ){
                            if ( !install_dirs.containsKey( spl2[0] ) ) {
                                String install = "";
                                if ( spl[1].Equals( "INSTALLDIR" ) ) {
                                    install = spl[2];
                                }
                                install_dirs.put( spl2[0], install );
                            } else {
                                if ( spl[1].Equals( "INSTALLDIR" ) ) {
                                    install_dirs.put( spl2[0], spl[2] );
                                }
                            }
                        }
                    }
                }
            }

            // installed_singersに追加
            for ( Iterator itr = install_dirs.keySet().iterator(); itr.hasNext(); ) {
                String id = (String)itr.next();
                String install = install_dirs.get( id );
                if ( id.Equals( "" ) ) {
                    install = path_voicedb.value;
                }
                installed_singers.add( install );
            }

            // path_expdbを取得
            Vector<String> exp_ids = new Vector<String>();
            // 最初はpath_expdbの取得と、id（BHXXXXXXXXXXXXXXXX）のようなシリアルを取得
            for ( Iterator itr = expression.iterator(); itr.hasNext(); ) {
                String s = (String)itr.next();
                String[] spl = PortUtil.splitString( s, new char[] { '\t' }, true );
                if ( spl.Length >= 3 ) {
                    if ( spl[1].Equals( "EXPRESSIONDIR" ) ) {
                        path_expdb.value = spl[2];
                    } else if ( spl.Length >= 3 ) {
                        String[] spl2 = PortUtil.splitString( spl[0], '\\' );
                        if ( spl2.Length == 1 ) {
                            if ( !exp_ids.contains( spl2[0] ) ) {
                                exp_ids.add( spl2[0] );
                            }
                        }
                    }
                }
            }
#if DEBUG
            PortUtil.println( "path_vsti=" + path_vsti.value );
            PortUtil.println( "path_voicedb=" + path_voicedb.value );
            PortUtil.println( "path_expdb=" + path_expdb.value );
            PortUtil.println( "installed_singers=" );
#endif
        }

        /// <summary>
        /// レジストリkey内の値を再帰的に検索し、ファイルfpに順次出力する
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent_name"></param>
        /// <param name="list"></param>
        private static void initPrint( String reg_key_name, String parent_name, Vector<String> list ) {
#if JAVA
#else
            try {
                RegistryKey key = Registry.LocalMachine.OpenSubKey( reg_key_name, false );
                if ( key == null ) {
                    return;
                }

                // 直下のキー内を再帰的にリストアップ
                String[] subkeys = key.GetSubKeyNames();
                foreach ( String s in subkeys ) {
                    initPrint( reg_key_name + "\\" + s, parent_name + "\\" + s, list );
                }

                // 直下の値を出力
                String[] valuenames = key.GetValueNames();
                foreach ( String s in valuenames ) {
                    RegistryValueKind kind = key.GetValueKind( s );
                    if ( kind == RegistryValueKind.String ) {
                        String str = parent_name + "\t" + s + "\t" + (String)key.GetValue( s, "" );
                        list.add( str );
                    }
                }
                key.Close();
            } catch ( Exception ex ) {
                PortUtil.stderr.println( "VocaloSysUtil#initPrint; ex=" + ex );
            }
#endif
        }

        /// <summary>
        /// アタック設定を順に返す反復子を取得します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Iterator attackConfigIterator( SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            if ( s_exp_config_sys.containsKey( type ) ) {
                return s_exp_config_sys.get( type ).attackConfigIterator();
            } else {
                return (new Vector<NoteHeadHandle>()).iterator();
            }
        }

        /// <summary>
        /// ビブラート設定を順に返す反復子を取得します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Iterator vibratoConfigIterator( SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            if ( s_exp_config_sys.containsKey( type ) ) {
                return s_exp_config_sys.get( type ).vibratoConfigIterator();
            } else {
                return (new Vector<VibratoHandle>()).iterator();
            }
        }

        /// <summary>
        /// 強弱記号設定を順に返す反復子を取得します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Iterator dynamicsConfigIterator( SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            if ( s_exp_config_sys.containsKey( type ) ) {
                return s_exp_config_sys.get( type ).dynamicsConfigIterator();
            } else {
                return (new Vector<IconDynamicsHandle>()).iterator();
            }
        }

        /// <summary>
        /// 指定した歌声合成システムに登録されている指定した名前の歌手について、その派生元の歌手名を取得します。
        /// </summary>
        /// <param name="singer"></param>
        /// <returns></returns>
        public static String getOriginalSinger( String singer, SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            String voiceidstr = "";
            if ( !s_singer_config_sys.containsKey( type ) ) {
                return "";
            }
            SingerConfigSys scs = s_singer_config_sys.get( type );
            SingerConfig[] singer_configs = scs.getSingerConfigs();
            for ( int i = 0; i < singer_configs.Length; i++ ) {
                if ( singer.Equals( singer_configs[i].VOICENAME ) ) {
                    voiceidstr = singer_configs[i].VOICEIDSTR;
                    break;
                }
            }
            if ( voiceidstr.Equals( "" ) ) {
                return "";
            }
            SingerConfig[] installed_singers = scs.getInstalledSingers();
            for ( int i = 0; i < installed_singers.Length; i++ ) {
                if ( voiceidstr.Equals( installed_singers[i].VOICEIDSTR ) ) {
                    return installed_singers[i].VOICENAME;
                }
            }
            return "";
        }

        /// <summary>
        /// 指定した歌声合成システムに登録されている指定した名前の歌手について、その歌手を表現するVsqIDのインスタンスを取得します。
        /// </summary>
        /// <param name="singer"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static VsqID getSingerID( String singer, SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            if ( !s_singer_config_sys.containsKey( type ) ) {
                return null;
            } else {
                return s_singer_config_sys.get( type ).getSingerID( singer );
            }
        }

        /// <summary>
        /// 指定した歌声合成システムの、エディタの実行ファイルのパスを取得します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static String getEditorPath( SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            if ( !s_path_editor.containsKey( type ) ) {
                return "";
            } else {
                return s_path_editor.get( type );
            }
        }

        /// <summary>
        /// 指定した歌声合成システムの、VSTi DLL本体のパスを取得します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static String getDllPathVsti( SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            if ( !s_path_vsti.containsKey( type ) ) {
                return "";
            } else {
                return s_path_vsti.get( type );
            }
        }

        /// <summary>
        /// 指定された歌声合成システムに登録されている歌手設定のリストを取得します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SingerConfig[] getSingerConfigs( SynthesizerType type ) {
            if ( !isInitialized ) {
                init();
            }
            if ( !s_singer_config_sys.containsKey( type ) ) {
                return new SingerConfig[] { };
            } else {
                return s_singer_config_sys.get( type ).getSingerConfigs();
            }
        }

        /// <summary>
        /// 指定した名前の歌手の歌唱言語を取得します。
        /// </summary>
        /// <param name="name">name of singer</param>
        /// <returns></returns>
        public static VsqVoiceLanguage getLanguageFromName( String name ) {
            if ( !isInitialized ) {
                init();
            }
            String search = name.ToLower();
            if ( search.Equals( "meiko" ) ||
                search.Equals( "kaito" ) ||
                search.Equals( "miku" ) ||
                search.Equals( "rin" ) ||
                search.Equals( "len" ) ||
                search.Equals( "rin_act2" ) ||
                search.Equals( "len_act2" ) ||
                search.Equals( "gackpoid" ) ||
                search.Equals( "luka_jpn" ) ||
                search.Equals( "megpoid" ) ||
                search.Equals( "sfa2_miki" ) ||
                search.Equals( "yuki" ) ||
                search.Equals( "kiyoteru" ) )
            {
                return VsqVoiceLanguage.Japanese;
            } else if ( search.Equals( "sweet_ann" ) ||
                search.Equals( "prima" ) ||
                search.Equals( "luka_eng" ) ||
                search.Equals( "sonika" ) ||
                search.Equals( "lola" ) ||
                search.Equals( "leon" ) ||
                search.Equals( "miriam" ) ||
                search.Equals( "big_al" ) )
            {
                return VsqVoiceLanguage.English;
            }
            return VsqVoiceLanguage.Japanese;
        }

        /// <summary>
        /// 指定したPAN値における、左チャンネルの増幅率を取得します。
        /// </summary>
        /// <param name="pan"></param>
        /// <returns></returns>
        public static double getAmplifyCoeffFromPanLeft( int pan ) {
            return pan / -64.0 + 1.0;
        }

        /// <summary>
        /// 指定したPAN値における、右チャンネルの増幅率を取得します。
        /// </summary>
        /// <param name="pan"></param>
        /// <returns></returns>
        public static double getAmplifyCoeffFromPanRight( int pan ) {
            return pan / 64.0 + 1.0;
        }

        /// <summary>
        /// 指定したFEDER値における、増幅率を取得します。
        /// </summary>
        /// <param name="feder"></param>
        /// <returns></returns>
        public static double getAmplifyCoeffFromFeder( int feder ) {
            return Math.Exp( -1.26697245e-02 + 1.18448420e-01 * feder / 10.0 );
        }

        /* // <summary>
        /// Transform the byte array(length=8) to unsigned long, assuming that the byte array is little endian.
        /// </summary>
        /// <param name="oct"></param>
        /// <returns></returns>
        public static long makelong_le( byte[] oct ) {
            return (long)oct[7] << 56 | (long)oct[6] << 48 | (long)oct[5] << 40 | (long)oct[4] << 32 | (long)oct[3] << 24 | (long)oct[2] << 16 | (long)oct[1] << 8 | (long)oct[0];
        }*/
    }

#if !JAVA
}
#endif
