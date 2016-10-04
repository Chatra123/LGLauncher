using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  using OctNov.IO;

  static class LogoSelector
  {
    /// <summary>
    /// LogoGuillo用　Logo、Param取得
    /// </summary>
    public static List<string> GetLogo_and_Param()
    {
      var result = Run();
      if (result == null)
        throw new LGLException("LogoSelector return value is null");
      if (result.Count < 2)
        throw new LGLException("LogoSelector return value is -lt 2 lines");

      string logoPath = result[0];
      string paramPath = result[1];
      return new List<string> { logoPath, paramPath };
    }


    /// <summary>
    /// Join_logo_Scp用  Logo取得
    /// </summary>
    public static List<string> GetLogo()
    {
      var result = Run();
      if (result == null)
        throw new LGLException("LogoSelector return value is null");
      if (result.Count < 1)
        throw new LGLException("LogoSelector return value is -lt 1 line");

      string logoPath = result[0];
      return new List<string> { logoPath };
    }


    /// <summary>
    /// LogoSelector実行
    /// </summary>
    private static List<string> Run()
    {
      if (File.Exists(PathList.LogoSelector) == false)
        throw new LGLException("LogoSelector does not exist");

      string path, args;
      {
        path = PathList.LogoSelector;
        args = string.Format("  \"{0}\"   \"{1}\"   \"{2}\"  ",
                              PathList.Channel, PathList.Program, PathList.TsPath);
      }

      //実行
      string result = Start_GetStdout(path, args);
      var split = result.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();

      //Log
      if (PathList.Is1stPart || PathList.IsAll)
      {
        var log = new StringBuilder();
        log.AppendLine("  [ LogoSelector ]");
        log.AppendLine("    path   :  " + path);
        log.AppendLine("    args   :  " + args);
        log.AppendLine("    return :" );
        log.Append(result);
        Log.WriteLine(log.ToString());
      }
      return split;
    }




    /// <summary>
    /// プロセス実行  標準出力を取得
    /// </summary>
    private static string Start_GetStdout(string exepath, string arg)
    {
      var prc = new Process();
      {
        prc.StartInfo.FileName = exepath;
        prc.StartInfo.Arguments = arg;

        //シェルコマンドを無効に、入出力をリダイレクトするなら必ずfalseに設定
        prc.StartInfo.UseShellExecute = false;
        //入出力のリダイレクト
        prc.StartInfo.RedirectStandardOutput = true;
      }

      try
      {
        //標準出力を取得
        prc.Start();
        string result = prc.StandardOutput.ReadToEnd();
        prc.WaitForExit();
        prc.Close();
        return result;
      }
      catch (Exception e)
      {
        Log.WriteLine(e.ToString());
        throw new LGLException("LogoSelector has error");
      }
    }


  }
}