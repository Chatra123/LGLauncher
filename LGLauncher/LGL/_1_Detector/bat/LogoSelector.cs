using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;


namespace LGLauncher.Bat
{
  /// <summary>
  /// Logo、Paramパス取得
  /// </summary>
  static class LogoSelector
  {
    public static string LogoPath { get; private set; }
    public static string ParamPath { get; private set; }
    public static bool HasLogo { get { return string.IsNullOrEmpty(LogoPath) == false; } }
    public static bool HasParam { get { return string.IsNullOrEmpty(ParamPath) == false; } }
    public static bool HasLogo_Param { get { return HasLogo && HasParam; } }
    public static string ResultLog { get; private set; }

    /// <summary>
    /// LogoSelector実行
    /// </summary>
    public static void Run(string exe, string ch, string program, string tsPath)
    {
      string args = string.Format("  \"{0}\"   \"{1}\"   \"{2}\"  ",
                              ch, program, tsPath);
      string stdout = Start_GetStdout(exe, args);
      var split = stdout.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
      if (1 <= split.Count) LogoPath = split[0];
      if (2 <= split.Count) ParamPath = split[1];

      var text = new StringBuilder();
      text.AppendLine("  [ LogoSelector ]");
      text.AppendLine("    path      :  " + exe);
      text.AppendLine("    args      :  " + args);
      text.AppendLine("    return    :");
      text.Append(stdout);
      text.AppendLine("    LogoPath  :  " + LogoPath);
      text.AppendLine("    ParamPath :  " + ParamPath);
      ResultLog = text.ToString();
    }


    /// <summary>
    /// プロセス実行  標準出力を取得
    /// </summary>
    private static string Start_GetStdout(string exepath, string arg)
    {
      var prc = new Process();
      prc.StartInfo.FileName = exepath;
      prc.StartInfo.Arguments = arg;
      //シェルコマンドを無効に、入出力をリダイレクトするなら必ずfalseに設定
      prc.StartInfo.UseShellExecute = false;
      //入出力のリダイレクト
      prc.StartInfo.RedirectStandardOutput = true;
      try
      {
        prc.Start();
        string stdout = prc.StandardOutput.ReadToEnd();
        prc.WaitForExit();
        prc.Close();
        return stdout;
      }
      catch (Exception e)
      {
        Log.WriteLine(e.ToString());
        throw new LGLException("LogoSelector has error");
      }
    }


  }
}