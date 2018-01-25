using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace LGLauncher
{

  static class Log
  {
    private static StreamWriter writer = null;

    /// <summary>
    /// ライター作成
    /// </summary>
    private static StreamWriter CreateWriter()
    {
      string LogPath = null;
      {
        //LogDir is
        //　LWorkDir    通常
        //　AppDir      LWorkDir作成前に例外が発生したとき
        var AppPath = Assembly.GetExecutingAssembly().Location;
        var AppDir = Path.GetDirectoryName(AppPath);
        var AppName = Path.GetFileNameWithoutExtension(AppPath);
        string LogDir = null;
        LogDir = LogDir ?? PathList.LWorkDir;
        LogDir = LogDir ?? AppDir;
        string LogName = PathList.TsShortName ?? AppName;
        LogPath = Path.Combine(LogDir, "_" + LogName + ".sys.log");
      }

      try
      {
        //追記、UTF-8 bom
        writer = new StreamWriter(LogPath, true, Encoding.UTF8);
      }
      catch { /*失敗*/ }

      //ヘッダー
      if (writer != null)
      {
        writer.AutoFlush = true;
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteLine(new string('=', 80));
        writer.WriteLine(DateTime.Now.ToString("G"));
      }
      return writer;
    }

    /// <summary>
    /// 閉
    /// </summary>
    public static void Close()
    {
      if (writer != null)
      {
        writer.Close();
        writer = null;
      }
    }


    /// <summary>
    /// 書
    /// </summary>
    public static void WriteLine(string line = "")
    {
      if (writer == null)
        writer = CreateWriter();
      if (writer != null)
        writer.WriteLine(line);
      Console.Error.WriteLine(line);
    }


  }

}