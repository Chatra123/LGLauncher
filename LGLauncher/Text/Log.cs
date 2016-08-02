using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace LGLauncher
{

  static class Log
  {
    public static bool Enable = true;
    private static StreamWriter writer = null;


    /// <summary>
    /// ライター作成
    /// </summary>
    private static StreamWriter CreateWriter()
    {
      string LogPath = null;
      {
        //Dirの使用想定
        //　LWorkDir    　　通常
        //　LTopWorkDir     LWorkDir    作成前に発生した LGLException用
        //　AppDir          LTopWorkDir 作成前に発生した LGLException用
        var AppPath = Assembly.GetExecutingAssembly().Location;
        var AppDir = Path.GetDirectoryName(AppPath);
        var AppName = Path.GetFileNameWithoutExtension(AppPath);
        string LogDir = null;
        LogDir = string.IsNullOrEmpty(LogDir) ? PathList.LWorkDir : LogDir;
        LogDir = string.IsNullOrEmpty(LogDir) ? PathList.LTopWorkDir : LogDir;
        LogDir = string.IsNullOrEmpty(LogDir) ? AppDir : LogDir;
        string LogName = string.IsNullOrEmpty(PathList.TsShortName)
          ? AppName : PathList.TsShortName;

        LogPath = Path.Combine(LogDir, "_" + LogName + ".sys.log");
      }

      try
      {
        writer = new StreamWriter(LogPath, true, Encoding.UTF8);       //追記、UTF-8 bom
      }
      catch { /*失敗*/ }


      //成功、ヘッダー書込み
      if (writer != null)
      {
        writer.AutoFlush = true;
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
        writer = null;
      }
    }


    /// <summary>
    /// テキストを書込む
    /// </summary>
    public static void WriteLine(string line = "")
    {
      if (Enable == false) return;
      if (writer == null) writer = CreateWriter();
      if (writer != null) writer.WriteLine(line);
    }


  }

}