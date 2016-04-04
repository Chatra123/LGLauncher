using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace LGLauncher
{
  using OctNov.IO;

  /// <summary>
  /// AbstractAvsMaker
  /// </summary>
  abstract class AbstractAvsMaker
  {
    public abstract int[] GetTrimFrame();
    public abstract string MakeTrimScript(int[] trimFrame);
  }

  /// <summary>
  /// AvsVpy作成
  /// </summary>
  class AvsVpyMaker
  {
    AbstractAvsMaker maker;
    static bool IndexHasFormatted = false;

    /// <summary>
    /// constructor
    /// </summary>
    public AvsVpyMaker()
    {
      //format d2v, lwi　
      if (IndexHasFormatted == false)
      {
        IndexHasFormatted = true;
        if (PathList.InputPlugin == PluginType.D2v)
        {
          D2vFormatter.Format();
        }
        else if (PathList.InputPlugin == PluginType.Lwi)
        {
          LwiFormatter.Format();
        }
      }

      maker =
        (PathList.AvsVpy == AvsVpyType.Avs) ? new AvsMaker() as AbstractAvsMaker :
        (PathList.AvsVpy == AvsVpyType.Vpy) ? new VpyMaker() as AbstractAvsMaker :
        null;

      if (PathList.AvsVpy == AvsVpyType.Vpy)
        throw new NotImplementedException();
    }

    /// <summary>
    /// トリムフレーム取得
    /// </summary>
    public int[] GetTrimFrame()
    {
      return maker.GetTrimFrame();
    }

    /// <summary>
    /// Trim付きスクリプト作成
    /// </summary>
    public string MakeTrimScript(int[] trimFrame)
    {
      return maker.MakeTrimScript(trimFrame);
    }
  }




  /// <summary>
  /// AvsVpyCommon
  /// </summary>
  static class AvsVpyCommon
  {

    #region InfoScript

    /// <summary>
    /// Info取得スクリプトを実行
    /// </summary>
    public static void RunInfo(Action action)
    {
      try
      {
        LwiFile.Set_ifLwi();
        action();
      }
      finally
      {
        LwiFile.Back_ifLwi();
      }
    }
    /// <summary>
    /// Info取得スクリプトを実行
    ///   mutexを取得して１つずつ実行する
    /// </summary>
    public static void RunInfo_withMutex(Action action)
    {
      WaitForSystemReady waitForReady = null;
      try
      {
        //mutex取得
        {
          waitForReady = new WaitForSystemReady();
          waitForReady.GetReady(PathList.DetectorName, 1, false);
        }

        try
        {
          LwiFile.Set_ifLwi();
          action();
        }
        finally
        {
          LwiFile.Back_ifLwi();
        }
      }
      finally
      {
        Thread.Sleep(3 * 1000);  //連続実行を抑止して負荷分散
        if (waitForReady != null)
          waitForReady.Release();
      }
    }


    /// <summary>
    /// *.info.txtからフレーム数を取得
    /// </summary>
    public static double[] GetInfoText(string infoPath)
    {
      var infoText = new List<string>();

      for (int retry = 1; retry <= 5; retry++)
      {
        //file check
        if (File.Exists(infoPath) == false)
        {
          Thread.Sleep(1000);
          continue;
        };

        infoText = FileR.ReadAllLines(infoPath);

        //line check
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

      //文字　→　数値
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
    /// トリムフレーム数取得
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

      //ファイル検索
      var files = Directory.GetFiles(PathList.LWorkDir, nameKey);
      if (files.Count() != 1)
      {
        Log.WriteLine("  Could'nt specify trim range." + "  file Count() = " + files.Count());
        Log.WriteLine("    nameKey = " + nameKey);
        return null;
      }

      //正規表現パターン
      //TsShortName.p1.0__1000.avs
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

    #endregion GetTrimFrame_fromName



    #region CreateDummy_OnError

    /// <summary>
    /// エラー発生時、次回のLGLancher実行で参照するavsファイル作成
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

      string dummyFilePath = string.Format(
        "{0}.{1}__{2}{3}",
        PathList.WorkPath,
        trimFrame_prv[1],
        trimFrame_prv[1],
        PathList.AvsVpyExt
        );

      File.Create(dummyFilePath).Close();

    }

    #endregion CreateDummy_OnError



    #region CalcTrimFrame

    /// <summary>
    /// トリム用フレーム数　計算
    /// </summary>
    /// <param name="totalframe">総フレーム数</param>
    /// <param name="trimFrame_prv">前回のトリムフレーム数</param>
    /// <returns>開始、終了フレーム数</returns>
    public static int[] CalcTrimFrame(int totalframe, int[] trimFrame_prv)
    {
      int beginFrame = 0, endFrame = 0;

      if (PathList.Is1stPart)
      {
        beginFrame = 0;
        endFrame = totalframe - 1;
      }
      else if (2 <= PathList.PartNo)
      {
        if (trimFrame_prv == null)
          throw new LGLException("previous trim frame is null");

        beginFrame = trimFrame_prv[1] + 1;                  //前回の終端フレーム数＋１
        endFrame = totalframe - 1;
      }
      else if (PathList.IsAll)
      {
        beginFrame = 0;
        endFrame = totalframe - 1;
      }

      return new int[] { beginFrame, endFrame };
    }

    #endregion GetTrimFrame



    #region OutScript

    /// <summary>
    /// Script出力
    /// </summary>
    public static string OutScript(int[] trimFrame, List<string> scriptText,
                                   string outExt, System.Text.Encoding enc)
    {
      //長さチェック
      //　 30frame以下だと logoGuilloの avs2pipemodがエラーで落ちる。
      //　120frame以下なら no frame errorと表示されて終了する。
      //　150frame以上に設定する。
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];
      int len = endFrame - beginFrame;

      //150 frame 以上か？
      if (30 * 5 <= len)
      {
        //書
        string outPath = string.Format("{0}.{1}__{2}{3}",
                                          PathList.WorkPath,
                                          beginFrame,
                                          endFrame,
                                          outExt);
        File.WriteAllLines(outPath, scriptText, enc);
        return outPath;
      }
      else
      {
        throw new LGLException("short video length.  -lt 150 frame");
      }
    }

    #endregion CreateTrimAvs


  }
}