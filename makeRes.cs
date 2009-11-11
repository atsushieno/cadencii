﻿using System;
using System.IO;
//using System.Drawing;

class makeRes{
    static string infile = "";
    static string outfile = "";
    static string package = "";
    static string name_space = "";

    public static void Main( string[] args ){
        // 引数を解釈
        string current = "";
        foreach( string s in args ){
            if( s.StartsWith( "-" ) ){
                current = s;
            }else{
                if( current == "-i" ){
                    infile = s;
                    current = "";
                }else if( current == "-o" ){
                    outfile = s;
                    current = "";
                }else if( current == "-p" ){
                    package = s;
                    current = "";
                }else if( current == "-n" ){
                    name_space = s;
                    current = "";
                }
            }
        }

        if( infile == "" || outfile == "" ){
            Console.WriteLine( "makeRes:" );
            Console.WriteLine( "    -i    input file" );
            Console.WriteLine( "    -o    output file" );
            Console.WriteLine( "    -p    package name [optional]" );
            Console.WriteLine( "    -n    namespace [optional]" );
            return;
        }
        if( !File.Exists( infile ) ){
            Console.WriteLine( "error; input file does not exists" );
            return;
        }

        using( StreamWriter sw = new StreamWriter( outfile ) )
        using( StreamReader sr = new StreamReader( infile ) ){
            string basedir = Path.GetDirectoryName( infile );
            // header
            string cs_space = (name_space == "" ? "" : "    ");
            sw.WriteLine( "#if JAVA" );
            if( package != "" ){
                sw.WriteLine( "package " + package + ";" );
                sw.WriteLine();
            }
            sw.WriteLine( "import java.awt.*;" );
            sw.WriteLine( "import javax.imageio.*;" );
            sw.WriteLine( "import javax.swing.*;" );
            sw.WriteLine( "import org.kbinani.*;" );
            sw.WriteLine( "#" + "else" );
            sw.WriteLine( "using System;" );
            sw.WriteLine( "using System.IO;" );
            sw.WriteLine( "using System.Drawing;" );
            sw.WriteLine( "using bocoree;" );
            sw.WriteLine();
            if( name_space != "" ){
                sw.WriteLine( "namespace " + name_space + "{" );
            }
            sw.WriteLine( "#endif" );
            sw.WriteLine();
            sw.WriteLine( cs_space + "public class Resources{" );
            sw.WriteLine( cs_space + "    private static String basePath = null;" );
            sw.WriteLine( cs_space + "    private static String getBasePath(){" );
            sw.WriteLine( cs_space + "        if( basePath == null ){" );
            sw.WriteLine( cs_space + "            basePath = PortUtil.combinePath( PortUtil.getApplicationStartupPath(), \"resources\" );" );
            sw.WriteLine( cs_space + "        }" );
            sw.WriteLine( cs_space + "        return basePath;" );
            sw.WriteLine( cs_space + "    }" );
            sw.WriteLine();
            string line = "";
            while( (line = sr.ReadLine()) != null ){
                string[] spl = line.Split( '\t' );
                if( spl.Length < 3 ){
                    continue;
                }
                string name = spl[0];
                string type = spl[1];
                string tpath = spl[2];
                string path = Path.Combine( basedir, tpath );
                if( !File.Exists( path ) ){
                    continue;
                }

                if( type == "Image" ){
                    string instance = "s_" + name;
                    string fname = Path.GetFileName( tpath );
                    sw.WriteLine( cs_space + "    private static Image " + instance + " = null;" );
                    sw.WriteLine( cs_space + "    public static Image get_" + name + "(){" );
                    sw.WriteLine( cs_space + "        if( " + instance + " == null ){" );
                    sw.WriteLine( cs_space + "            try{" );
                    sw.WriteLine( cs_space + "                String res_path = PortUtil.combinePath( getBasePath(), \"" + fname + "\" );" );
                    sw.WriteLine( "#if JAVA" );
                    sw.WriteLine( cs_space + "                " + instance + " = ImageIO.read( new File( res_path ) );" );
                    sw.WriteLine( "#else" );
                    sw.WriteLine( cs_space + "                " + instance + " = new Bitmap( res_path );" );
                    sw.WriteLine( "#endif" );
                    sw.WriteLine( cs_space + "            }catch( Exception ex ){" );
                    sw.WriteLine( cs_space + "            }" );
                    sw.WriteLine( cs_space + "        }" );
                    sw.WriteLine( cs_space + "        return " + instance + ";" );
                    sw.WriteLine( cs_space + "    }" );
                    sw.WriteLine();
                }else if( type == "Icon" ){
                    string instance = "s_" + name;
                    string fname = Path.GetFileName( tpath );
                    sw.WriteLine( cs_space + "    private static Icon " + instance + " = null;" );
                    sw.WriteLine( cs_space + "    public static Icon get_" + name + "(){" );
                    sw.WriteLine( cs_space + "        if( " + instance + " == null ){" );
                    sw.WriteLine( cs_space + "            try{" );
                    sw.WriteLine( cs_space + "                String res_path = PortUtil.combinePath( getBasePath(), \"" + fname + "\" );" );
                    sw.WriteLine( "#if JAVA" );
                    sw.WriteLine( cs_space + "                Image img = ImageIO.read( new File( res_path ) );" );
                    sw.WriteLine( cs_space + "                " + instance + " = new ImageIcon( img );" );
                    sw.WriteLine( "#else" );
                    sw.WriteLine( cs_space + "                " + instance + " = new Icon( res_path );" );
                    sw.WriteLine( "#endif" );
                    sw.WriteLine( cs_space + "            }catch( Exception ex ){" );
                    sw.WriteLine( cs_space + "            }" );
                    sw.WriteLine( cs_space + "        }" );
                    sw.WriteLine( cs_space + "        return " + instance + ";" );
                    sw.WriteLine( cs_space + "    }" );
                    sw.WriteLine();
                }else if( type == "Cursor" ){
                    string instance = "s_" + name;
                    string fname = Path.GetFileName( tpath );
                    sw.WriteLine( cs_space + "    private static Cursor " + instance + " = null;" );
                    sw.WriteLine( cs_space + "    public static Cursor get_" + name + "(){" );
                    sw.WriteLine( cs_space + "        if( " + instance + " == null ){" );
                    sw.WriteLine( cs_space + "            try{" );
                    sw.WriteLine( cs_space + "                String res_path = PortUtil.combinePath( getBasePath(), \"" + fname + "\" );" );
                    sw.WriteLine( "#if JAVA" );
                    sw.WriteLine( cs_space + "                Image img = ImageIO.read( new File( res_path ) );" );
                    sw.WriteLine( cs_space + "                " + instance + " = Toolkit.getDefaultToolkit().createCustomCursor( img, new Point( 0, 0 ), \"" + name + "\" );" );
                    sw.WriteLine( "#else" );
                    sw.WriteLine( cs_space + "                FileStream fs = null;" );
                    sw.WriteLine( cs_space + "                try{" );
                    sw.WriteLine( cs_space + "                    fs = new FileStream( res_path, FileMode.Open, FileAccess.Read );" );
                    sw.WriteLine( cs_space + "                    " + instance + " = new Cursor( fs );" );
                    sw.WriteLine( cs_space + "                }catch( Exception ex0 ){" );
                    sw.WriteLine( cs_space + "                }finally{" );
                    sw.WriteLine( cs_space + "                    if( fs != null ){" );
                    sw.WriteLine( cs_space + "                        try{" );
                    sw.WriteLine( cs_space + "                            fs.Close();" );
                    sw.WriteLine( cs_space + "                        }catch( Exception ex2 ){" );
                    sw.WriteLine( cs_space + "                        }" );
                    sw.WriteLine( cs_space + "                    }" );
                    sw.WriteLine( cs_space + "                }" );
                    sw.WriteLine( "#endif" );
                    sw.WriteLine( cs_space + "            }catch( Exception ex ){" );
                    sw.WriteLine( cs_space + "            }" );
                    sw.WriteLine( cs_space + "        }" );
                    sw.WriteLine( cs_space + "        return " + instance + ";" );
                    sw.WriteLine( cs_space + "    }" );
                    sw.WriteLine();
                }
            }
            sw.WriteLine( cs_space + "}" );
            if( name_space != "" ){
                sw.WriteLine( "#if !JAVA" );
                sw.WriteLine( "}" );
                sw.WriteLine( "#endif" );
            }
        }
    }
}