using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace LGLauncher
{

  //======================================
  //  ログ
  //======================================
  #region Log
  static class Log
  {
    public static bool Enable = true;
    static StreamWriter writer = null;

    //
    //  ライター作成
    static StreamWriter CreateWriter()
    {
      //アプリ
      var AppPath = Assembly.GetExecutingAssembly().Location;
      var AppDir = Path.GetDirectoryName(AppPath);
      var AppName = Path.GetFileNameWithoutExtension(AppPath);


      //ファルダ名作成   LogDir優先順位　（高）WorkDir、TopWorkDir、AppDir　（低）
      string LogDir = null;
      LogDir = (string.IsNullOrEmpty(LogDir) == false) ? LogDir : PathList.LWorkDir;     //LWorkDir
      LogDir = (string.IsNullOrEmpty(LogDir) == false) ? LogDir : PathList.LTopWorkDir;  //LTopWorkDir
      LogDir = (string.IsNullOrEmpty(LogDir) == false) ? LogDir : AppDir;                //AppDir
      bool IsAppLog = (LogDir != PathList.LWorkDir);


      //ファイル名作成
      string LogName = string.IsNullOrEmpty(PathList.TsShortName) ? AppName : PathList.TsShortName;

      //ライター作成
      //　*.sys.1.log ～ *.sys.16.logを割り当てる。
      for (int i = 1; i <= 16; i++)
      {
        try
        {
          var path = Path.Combine(LogDir, "_" + LogName + ".sys." + i + ".log");
          var logfile = new FileInfo(path);
          bool append = true;                              //追記
          if (IsAppLog)                                    //IsAppLog ＆ ２ＫＢ以上なら上書き
            if (logfile.Exists && 2 * 1024 <= logfile.Length)
              append = false;                              //上書き

          writer = new StreamWriter(path, append, new UTF8Encoding(true));
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

    //閉じる
    public static void Close() { if (writer != null) { writer.Close(); } }


    //書込み
    public static void WriteLine(string line = "")
    {
      if (Enable == false) return;
      if (writer == null) { writer = CreateWriter(); }
      if (writer != null) { writer.WriteLine(line); writer.Flush(); }
    }


  }
  #endregion



}
