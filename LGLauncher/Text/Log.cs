using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace LGLauncher
{

  internal static class Log
  {
    public static bool Enable = true;
    private static StreamWriter writer = null;


    /// <summary>
    /// ライター作成
    /// </summary>
    private static StreamWriter CreateWriter()
    {
      string LogDir = null, LogName = null;
      {
        //Dirの使用想定
        //　LWorkDir    　　通常用
        //　LTopWorkDir     LWorkDir    作成前に発生した LGLException用
        //　AppDir          LTopWorkDir 作成前に発生した LGLException用
        var AppPath = Assembly.GetExecutingAssembly().Location;
        var AppDir = Path.GetDirectoryName(AppPath);
        var AppName = Path.GetFileNameWithoutExtension(AppPath);

        //ファルダ名
        LogDir = null;
        LogDir = (string.IsNullOrEmpty(LogDir)) ? PathList.LWorkDir : LogDir;
        LogDir = (string.IsNullOrEmpty(LogDir)) ? PathList.LTopWorkDir : LogDir;
        LogDir = (string.IsNullOrEmpty(LogDir)) ? AppDir : LogDir;

        //ファイル名
        LogName = string.IsNullOrEmpty(PathList.TsShortName)
                                   ? AppName : PathList.TsShortName;
      }


      //ライター作成
      //　*.sys.1.log ～ *.sys.4.logを割り当てる。
      //  基本的に *.sys.1.logのみ使用する。
      for (int i = 1; i <= 4; i++)
      {
        try
        {
          var path = Path.Combine(LogDir, "_" + LogName + ".sys." + i + ".log");
          var logfile = new FileInfo(path);

          writer = new StreamWriter(path, true, Encoding.UTF8);       //追記、UTF-8 bom
          break;
        }
        catch { /*ファイル使用中*/ }
      }

      //作成成功、ヘッダー書込み
      if (writer != null)
      {
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteLine(new String('=', 80));
        writer.WriteLine(DateTime.Now.ToString("G"));
      }

      return writer;
    }

    /// <summary>
    /// 閉じる
    /// </summary>
    public static void Close()
    {
      if (writer != null)
      {
        WriteLine("  exit");
        writer.Close();
      }
    }


    /// <summary>
    /// テキストを書込む
    /// </summary>
    /// <param name="line">書込むテキスト</param>
    public static void WriteLine(string line = "")
    {
      if (Enable == false) return;
      if (writer == null) { writer = CreateWriter(); }
      if (writer != null) { writer.WriteLine(line); writer.Flush(); }  //AutoFlush
    }


  }

}