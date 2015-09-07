using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace LGLauncher
{
  //======================================
  //  ログ
  //======================================

  #region Log

  internal static class Log
  {
    public static bool Enable = true;
    private static StreamWriter writer = null;

    /// <summary>
    /// ライター作成
    /// </summary>
    /// <returns></returns>
    private static StreamWriter CreateWriter()
    {
      string LogDir = new Func<string>(() =>
      {
        var AppPath = Assembly.GetExecutingAssembly().Location;
        var AppDir = Path.GetDirectoryName(AppPath);
        var AppName = Path.GetFileNameWithoutExtension(AppPath);

        //ファルダ名作成
        //  LogDir優先順位　（高）  LWorkDir、LTopWorkDir、AppDir  （低）
        string dir = null;
        dir = (string.IsNullOrEmpty(dir) == false) ? dir : PathList.LWorkDir;
        dir = (string.IsNullOrEmpty(dir) == false) ? dir : PathList.LTopWorkDir;
        dir = (string.IsNullOrEmpty(dir) == false) ? dir : AppDir;
        return dir;

        //dirの使用状況の想定
        //　LWorkDir    　　通常
        //　LTopWorkDir     d2vが作成されていないときはLWorkDir作成前に
        //　　　　　　　　　例外がスローされる。
        //　AppDir          LTopWorkDir作成前に発生した例外用
      })();

      string LogName = string.IsNullOrEmpty(PathList.TsShortName)
                                 ? "LGLauncher" : PathList.TsShortName;

      //ライター作成
      //　*.sys.1.log ～ *.sys.16.logを割り当てる。
      for (int i = 1; i <= 16; i++)
      {
        try
        {
          var path = Path.Combine(LogDir, "_" + LogName + ".sys." + i + ".log");
          var logfile = new FileInfo(path);

          writer = new StreamWriter(path, true, new UTF8Encoding(true));       //追記
          break;
        }
        catch { }  //オープン失敗。別プロセスがファイル使用中
      }

      //ライター作成成功、ヘッダー書込み
      if (writer != null)
      {
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteLine("================================================================================");
        writer.WriteLine(DateTime.Now.ToString("G"));
      }

      return writer;
    }

    /// <summary>
    /// 閉じる
    /// </summary>
    public static void Close()
    {
      if (writer != null) { writer.Close(); }
    }

    /// <summary>
    /// テキストを書込む
    /// </summary>
    /// <param name="line">書込むテキスト</param>
    public static void WriteLine(string line = "")
    {
      if (Enable == false) return;
      if (writer == null) { writer = CreateWriter(); }
      if (writer != null) { writer.WriteLine(line); writer.Flush(); }
    }

    /// <summary>
    /// 例外情報を書込む
    /// </summary>
    /// <param name="e"></param>
    public static void WriteException(Exception e)
    {
      var msglist = e.ToString().Split(new string[] { "場所", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(line => !string.IsNullOrWhiteSpace(line));

      var sb = new StringBuilder();
      sb.AppendLine("  /▽  Error  ▽/");

      foreach (var line in msglist)
      {
        sb.AppendLine("    →  " + line.Replace(Environment.NewLine, ""));
      }

      WriteLine(sb.ToString());
    }
  }

  #endregion Log
}