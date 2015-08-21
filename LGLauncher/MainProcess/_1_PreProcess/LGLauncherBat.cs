using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  internal static class BatLogoGuillo
  {
    /// <summary>
    /// LogoGuillo起動用バッチ作成
    /// </summary>
    /// <param name="avsPath">LogoGuilloに渡すavsファイルパス</param>
    /// <param name="srtPath">LogoGuilloに渡すsrtファイルパス</param>
    /// <returns></returns>
    static public string Make(string avsPath, string srtPath)
    {
      //引数チェック
      if (File.Exists(avsPath) == false)
        throw new LGLException();           //avsがなければ終了

      //srtをavsと同じ名前にリネーム
      if (File.Exists(srtPath))
      {
        string avsWithoutExt = Path.GetFileNameWithoutExtension(avsPath);
        string newSrtPath = Path.Combine(PathList.LWorkDir, avsWithoutExt + ".srt");

        try
        {
          if (File.Exists(newSrtPath)) File.Delete(newSrtPath);
          File.Move(srtPath, newSrtPath);
        }
        catch { }
      }

      //BaseLGLauncher.bat読込み
      var batText = new List<string>();
      batText = FileR.ReadFromResource("LGLauncher.ResourceText.BaseLGLauncher.bat");

      //ロゴデータ取得
      var logoAndParam = GetLogoAndParam(PathList.Channel, PathList.Program, PathList.TsPath);

      if (logoAndParam == null) 
        throw new LGLException("logoAndParam is null");

      if (logoAndParam.Count < 2) 
        throw new LGLException("logoAndParam is not detect");

      string logoPath = logoAndParam[0];
      string paramPath = logoAndParam[1];

      if (File.Exists(logoPath) == false)
        throw new LGLException("LogoPath is not exist");

      if (File.Exists(paramPath) == false) 
        throw new LGLException("ParamPath is not exist");

      if (File.Exists(PathList.LogoGuillo) == false) 
        throw new LGLException("LogoGuillo is not exist");

      //#LOGOG_PATH#
      string LOGOG_PATH = @"..\..\LSystem\LogoGuillo.exe";
      //#AVS2X_PATH#
      string AVS2X_PATH = @"..\..\LSystem\avs2pipemod.exe";
      //#AVSPLG_PATH#
      string AVSPLG_PATH = @"..\..\LWork\USE_AVS";
      //#VIDEO_PATH#
      string VIDEO_PATH = avsPath;     //相対パスだとLogoGuilloの作業フォルダから検索される。フルパスで指定
      //#LOGO_PATH#
      string LOGO_PATH = logoPath;
      //#PRM_PATH#
      string PRM_PATH = paramPath;
      //#OUTPUT_PATH#
      string OUTPUT_PATH = PathList.WorkName + ".frame.txt";

      //bat書き換え
      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        //LGL
        line = Regex.Replace(line, "#WorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#PartNo#", "" + PathList.No, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#TsShortName#", PathList.TsShortName, RegexOptions.IgnoreCase);
        //LOGOG
        line = Regex.Replace(line, "#LOGOG_PATH#", LOGOG_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#AVS2X_PATH#", AVS2X_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#AVSPLG_PATH#", AVSPLG_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#VIDEO_PATH#", VIDEO_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#LOGO_PATH#", LOGO_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#PRM_PATH#", PRM_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#OUTPUT_PATH#", OUTPUT_PATH, RegexOptions.IgnoreCase);
        batText[i] = line;
      }

      //出力ファイル名
      string outBatPath;
      outBatPath = PathList.WorkPath + ".bat";

      //ファイル書込み
      File.WriteAllLines(outBatPath, batText, TextEnc.Shift_JIS);

      return outBatPath;
    }

    #region GetLogoAndParam

    /// <summary>
    /// ロゴデータのパス取得
    /// </summary>
    /// <param name="channel">LogoSelectorに渡すチャンネル名</param>
    /// <param name="program">LogoSelectorに渡すプログラム名</param>
    /// <param name="tsPath">LogoSelectorに渡すTSパス</param>
    /// <returns>ロゴ、パラメーターパス</returns>
    private static List<string> GetLogoAndParam(string channel, string program, string tsPath)
    {
      //ファイルチェック
      if (File.Exists(PathList.LogoSelector) == false)
        throw new LGLException("LogoSelector is not exist");

      //パス、引数
      string exepath = "", arg = "";
      var ext = Path.GetExtension(PathList.LogoSelector).ToLower();

      if (ext == ".exe")
      {
        //LogoSelector.exe
        exepath = PathList.LogoSelector;
        arg = string.Format("  \"{0}\"   \"{1}\"   \"{2}\"  ",
                              channel, program, tsPath);
      }
      else if (ext == ".vbs" || ext == ".js")
      {
        //LogoSelector.vbs  LogoSelector.js
        exepath = "cscript.exe";
        arg = string.Format("  \"{0}\"   \"{1}\"   \"{2}\"   \"{3}\"  ",
                              PathList.LogoSelector, channel, program, tsPath);
      }
      else
        exepath = "ext does not correspond";

      //実行
      Log.WriteLine();
      Log.WriteLine("LogoSelector:");
      Log.WriteLine(exepath);
      Log.WriteLine("arg    :");
      Log.WriteLine(arg);

      string result = Get_stdout(exepath, arg);

      Log.WriteLine("return :");
      Log.WriteLine(result);

      var split = result.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();

      return split;
    }

    #endregion GetLogoAndParam

    #region Get_stdout

    /// <summary>
    /// プロセス実行  標準出力を読み取る
    /// </summary>
    /// <param name="exepath">実行ファイルパス</param>
    /// <param name="arg">実行ファイルに渡す引数</param>
    /// <returns>取得した標準出力</returns>
    private static string Get_stdout(string exepath, string arg)
    {
      var prc = new Process();
      prc.StartInfo.FileName = exepath;
      prc.StartInfo.Arguments = arg;

      //シェルコマンドを無効に、入出力をリダイレクトするなら必ずfalseに設定
      prc.StartInfo.UseShellExecute = false;
      prc.StartInfo.RedirectStandardOutput = true;         //入出力のリダイレクト

      try
      {
        prc.Start();
        //標準出力を読み取る、プロセス終了まで待機
        string result = prc.StandardOutput.ReadToEnd();
        prc.WaitForExit();
        prc.Close();
        return result;
      }
      catch (Exception exc)
      {
        //例外をそのまま返してログに書き込んでもらう。
        return exc.ToString();
      }
    }

    #endregion Get_stdout
  }
}