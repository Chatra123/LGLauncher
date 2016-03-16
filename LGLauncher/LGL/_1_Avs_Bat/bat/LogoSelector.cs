using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  using OctNov.IO;

  internal static class LogoSelector
  {
    /// <summary>
    /// LogoGuillo用　Logo、Param取得
    /// </summary>
    public static List<string> GetLogo_and_Param()
    {
      var getdata = RunLogoSelector();

      if (getdata == null)
        throw new LGLException("LogoSelector ret data is null");
      if (getdata.Count < 2)
        throw new LGLException("LogoSelector ret data is -lt 2 lines");

      string logoPath = getdata[0];
      string paramPath = getdata[1];
      return new List<string> { logoPath, paramPath };
    }


    /// <summary>
    /// Join_logo_Scp用    Logo取得
    /// </summary>
    public static List<string> GetLogo()
    {
      var getdata = RunLogoSelector();

      if (getdata == null)
        throw new LGLException("LogoSelector ret data is null");
      if (getdata.Count < 1)
        throw new LGLException("LogoSelector ret data is -lt 1 lines");

      string logoPath = getdata[0];
      return new List<string> { logoPath };

    }


    /// <summary>
    /// 未実装　 Join_logo_Scpos用    Cmdファイル取得
    /// </summary>
    public static List<string> GetJLCmd()
    {
      //未実装
      throw new NotImplementedException();
    }


    /// <summary>
    /// LogoSelector実行
    /// </summary>
    private static List<string> RunLogoSelector()
    {
      if (File.Exists(PathList.LogoSelector) == false)
        throw new LGLException("LogoSelector does not exist");

      //パス、引数
      string exepath = PathList.LogoSelector;
      string args = string.Format("  \"{0}\"   \"{1}\"   \"{2}\"  ", 
        PathList.Channel, PathList.Program, PathList.TsPath);
      SetScriptLoader(ref exepath, ref args);

      //実行
      string result;
      bool success = Start_GetStdout(exepath, args, out result);
      var split = result.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();

      //Log
      if (PathList.Is1stPart || PathList.IsAll || success == false)
      {
        Log.WriteLine("      LogoSelector :");
        Log.WriteLine(exepath);
        Log.WriteLine("              args :");
        Log.WriteLine(args);
        Log.WriteLine("            return :");
        Log.WriteLine(result);
      }
      return split;
    }


    /// <summary>
    /// vbsがセットされていたらcscript.exeに変更
    /// batは変更しなくても処理できる。
    /// </summary>
    private static void SetScriptLoader(ref string exepath, ref string args)
    {
      var ext = System.IO.Path.GetExtension(exepath).ToLower();
      if (ext == ".vbs" || ext == ".js")
      {
        string scriptPath = exepath;
        exepath = "cscript.exe";
        args = string.Format(" \"{0}\"  {1} ", scriptPath, args);
      }
    }


    /// <summary>
    /// プロセス実行  標準出力を取得
    /// </summary>
    private static bool Start_GetStdout(string exepath, string arg, out string result)
    {
      var Process = new Process();
      Process.StartInfo.FileName = exepath;
      Process.StartInfo.Arguments = arg;

      //シェルコマンドを無効に、入出力をリダイレクトするなら必ずfalseに設定
      Process.StartInfo.UseShellExecute = false;
      //入出力のリダイレクト
      Process.StartInfo.RedirectStandardOutput = true;

      try
      {
        //標準出力を取得、プロセス終了まで待機
        Process.Start();
        result = Process.StandardOutput.ReadToEnd();
        Process.WaitForExit();
        Process.Close();
        return true;
      }
      catch (Exception exc)
      {
        //例外を返してログに書き込んでもらう。
        result = exc.ToString();
        return false;
      }
    }


  }
}