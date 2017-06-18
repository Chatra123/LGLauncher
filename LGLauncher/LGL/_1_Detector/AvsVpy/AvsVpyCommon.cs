﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;


namespace LGLauncher
{
  using OctNov.IO;

  /// <summary>
  /// Avs or Vpy作成
  /// </summary>
  class AvsVpyMaker
  {
    AbstractAvsMaker maker;

    /// <summary>
    /// constructor
    /// </summary>
    public AvsVpyMaker()
    {
      IndexFormatter.Format(PathList.IsD2v);
      maker = PathList.IsAvs ? new AvsMaker() as AbstractAvsMaker :
              PathList.IsVpy ? new VpyMaker() as AbstractAvsMaker :
              null;
    }

    /// <summary>
    /// トリムフレーム取得
    /// </summary>
    public int[] GetTrimFrame()
    {
      return maker.GetTrimFrame();
    }

    /// <summary>
    /// スクリプト作成
    /// </summary>
    public string MakeScript(int[] trimFrame)
    {
      return maker.MakeScript(trimFrame);
    }
  }

  /// <summary>
  /// Avs, Vpyの抽象化
  /// </summary>
  abstract class AbstractAvsMaker
  {
    public abstract int[] GetTrimFrame();
    public abstract string MakeScript(int[] trimFrame);
  }




  /// <summary>
  /// AvsMaker,VpyMakerの共通部分
  /// </summary>
  static class AvsVpyCommon
  {

    #region InfoScript

    /// <summary>
    /// *.info.txtからフレーム数を取得
    /// </summary>
    public static double[] GetInfo_fromText(string infoPath)
    {
      var infoText = new List<string>();

      for (int retry = 1; retry <= 3; retry++)
      {
        if (File.Exists(infoPath) == false)
        {
          Thread.Sleep(2000);
          continue;
        };

        infoText = FileR.ReadAllLines(infoPath);
        //line count check
        if (infoText == null || infoText.Count <= 1)
        {
          Thread.Sleep(500);
          continue;
        }
        else
          break;   //取得成功
      }
      if (infoText == null || infoText.Count <= 1)
        throw new LGLException("info file is invalid");

      try
      {
        double frame = double.Parse(infoText[0]);
        return new double[] { frame };
      }
      catch
      {
        throw new LGLException("info parse error");
      }
    }

    #endregion InfoScript



    #region GetTrimFrame

    /// <summary>
    /// 今回のトリムフレーム数取得
    /// </summary>
    public static int[] GetTrimFrame()
    {
      return GetTrimFrame_fromName(PathList.WorkName + ".*__*" + PathList.AvsVpyExt);
    }

    /// <summary>
    /// 前回のトリムフレーム数取得
    /// </summary>
    public static int[] GetTrimFrame_previous()
    {
      return GetTrimFrame_fromName(PathList.WorkName_prv + ".*__*" + PathList.AvsVpyExt);
    }

    /// <summary>
    /// ファイル名からトリムフレーム数取得
    /// </summary>
    /// <param name="nameKey">対象のファイル名。ワイルドカード指定可</param>
    /// <returns>開始、終了フレーム数</returns>
    private static int[] GetTrimFrame_fromName(string nameKey)
    {
      if (PathList.LWorkDir == null) return null;

      var files = Directory.GetFiles(PathList.LWorkDir, nameKey);
      if (files.Count() != 1)
      {
        Log.WriteLine("  Could'nt specify trim range." + "  file Count() = " + files.Count());
        Log.WriteLine("    nameKey = " + nameKey);
        return null;
      }

      //TsShortName.p1.0__1000.avs
      //TsShortName.all.0__1000.avs
      //  <begin>      0
      //  <end>     1000
      var regex = new Regex(@".*\.(?<begin>\d+)__(?<end>\d+)\.[(avs)|(vpy)]", RegexOptions.IgnoreCase);

      Match match = regex.Match(files[0]);
      if (match.Success)
      {
        //文字　→　数値
        string sbegin = match.Groups["begin"].Value;
        string send = match.Groups["end"].Value;
        try
        {
          int ibegin = int.Parse(sbegin);
          int iend = int.Parse(send);
          return new int[] { ibegin, iend };
        }
        catch
        {
          // parse error
          return null;
        }
      }
      else  // match error
        return null;
    }

    #endregion GetTrimFrame



    #region OutScript

    /// <summary>
    /// Scriptファイル作成
    /// </summary>
    public static string OutScript(int[] trimFrame, List<string> scriptText,
                                   string ext, Encoding enc)
    {
      //長さチェック
      //  30frame以下だと logoGuilloの avs2pipemodがエラーで落ちる。
      //  120frame以下なら no frame errorと表示されて終了する。
      //  150frame以上に設定する。
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];
      int len = endFrame - beginFrame + 1;
      if (150 <= len)
      {
        //書
        string outPath = string.Format("{0}.{1}__{2}{3}",
                                       PathList.WorkPath,
                                       beginFrame,
                                       endFrame,
                                       ext);
        File.WriteAllLines(outPath, scriptText, enc);
        return outPath;
      }
      else
      {
        throw new LGLException("short video length.  -lt 150 frame");
      }
    }

    #endregion OutScript



    #region CreateDummy_OnError

    /// <summary>
    /// エラー発生時、次回のLGLancher実行で参照するavsファイルを作成
    /// </summary>
    /// <remarks>
    /// ・作成済みのavsファイルはあらかじめ削除しておくこと。
    /// ・TrimFrame数を進めないために　trimFrame_prv[1], trimFrame_prv[1]　にする。
    /// 
    /// 　TsShortName.p2.1000__2000.avs　を削除しておき
    /// 　TsShortName.p2.1000__1000.avs　を作成 
    /// </remarks>
    public static void CreateDummy_OnError()
    {
      int[] trimFrame_prv;
      {
        if (PathList.IsAll || PathList.Is1stPart)
        {
          trimFrame_prv = new int[] { 0, 0 };
        }
        else
        {
          trimFrame_prv = GetTrimFrame_previous();
          if (trimFrame_prv == null) return;
        }
      }

      string path = string.Format("{0}.{1}__{2}{3}",
                                  PathList.WorkPath,
                                  trimFrame_prv[1],
                                  trimFrame_prv[1],
                                  PathList.AvsVpyExt
                                  );
      File.Create(path).Close();
    }

    #endregion CreateDummy_OnError


  }
}