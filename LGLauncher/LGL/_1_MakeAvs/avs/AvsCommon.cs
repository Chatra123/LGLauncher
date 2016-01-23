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

  internal abstract class AbstractAvsMaker
  {
    public abstract string AvsPath { get; protected set; }            //作成したAVSのパス
    public abstract int[] TrimFrame { get; protected set; }           //トリム用フレーム数

    public abstract void Make();

  }

  internal static class MakeAvsCommon
  {

    #region RunInfoAvs

    /// <summary>
    /// InfoAvs実行
    /// </summary>
    /// <param name="avsPath">実行するavsパス</param>
    public static void RunInfoAvs(string avsPath)
    {
      ////デバッグ用
      //// 1/x の確立で例外を発生させる
      //if (DateTime.Now.Second % 3 == 0)
      //  throw new LGLException("fake error:  RunInfoAvs error");


      //
      // avs2pipeでのエラー発生を考慮して一度だけリトライする。
      //
      for (int retry = 1; retry <= 1; retry++)
      {
        var psi = new ProcessStartInfo();
        psi.FileName = PathList.avs2pipemod;
        psi.Arguments = " -info \"" + avsPath + "\"";
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        var prc = new Process();
        prc.StartInfo = psi;

        //実行
        if (prc.Start() == false)
          throw new LGLException("avsinfo launch error");

        //３秒程はかかる
        prc.WaitForExit(120 * 1000);

        //正常終了ならreturn
        if (prc.HasExited && 
          prc.ExitCode == 0) return;

        Thread.Sleep(10 * 1000);
      }

      Log.WriteLine("avsinfo process error");
    }

    #endregion RunInfoAvs


    #region GetAvsInfo

    /// <summary>
    /// *.info.txtからフレーム数、fps、時間を取得
    /// </summary>
    /// <param name="infoName">対象のinfoテキストのパス</param>
    /// <returns>フレーム数、fps、時間</returns>
    public static double[] GetAvsInfo(string infoName)
    {
      //get file
      string infoPath = Path.Combine(PathList.LWorkDir, infoName);
      var infoText = new List<string>();

      for (int retry = 1; retry <= 10; retry++)
      {
        //ファイルチェック
        if (File.Exists(infoPath) == false)
        {
          Thread.Sleep(2000);
          continue;
        };

        //読込み
        infoText = FileR.ReadAllLines(infoPath);

        //行数チェック
        if (infoText == null || infoText.Count < 4)
        {
          Thread.Sleep(2000);
          continue;
        }
        else
          break;   //取得成功
      }

      if (infoText == null || infoText.Count < 4)
        throw new LGLException("avsinfo file is invalid");

      //文字　→　数値
      double frame, fps, time;
      try
      {
        frame = double.Parse(infoText[0]);
        fps = double.Parse(infoText[1]);
        time = double.Parse(infoText[2]);
        return new double[] { frame, fps, time };
      }
      catch
      {
        throw new LGLException("avsinfo parse error");
      }

    }

    #endregion GetAvsInfo


    #region GetTrimFrame_fromAvsName

    /// <summary>
    /// 指定キーのファイル名からトリムフレーム数取得
    /// </summary>
    /// <param name="nameKey">対象のファイル名。ワイルドカード指定可</param>
    /// <returns>開始、終了フレーム数</returns>
    public static int[] GetTrimFrame_fromAvsName(string nameKey)
    {
      //ファイル検索
      var files = Directory.GetFiles(PathList.LWorkDir, nameKey);
      if (files.Count() != 1)
      {
        Log.WriteLine("avs file Count() = " + files.Count() + "  Could'nt specify previous trim range.");
        return null;
      }

      //正規表現パターン
      //TsShortName.p1.d2v_0__1000.avs
      //TsShortName.p1.lwi_0__1000.avs
      //  <begin>      0
      //  <end>     1000
      var regex = new Regex(@".*\.\w+_(?<begin>\d+)__(?<end>\d+)\.avs", RegexOptions.IgnoreCase);

      Match match = regex.Match(files[0]);

      //成功
      if (match.Success)
      {
        //文字　→　数値
        string sbegin = match.Groups["begin"].Value;
        string send = match.Groups["end"].Value;
        int ibegin, iend;

        try
        {
          ibegin = int.Parse(sbegin);
          iend = int.Parse(send);
          return new int[] { ibegin, iend };
        }
        catch
        {
          // parse error
          return null;
        }
      }
      else  // filename match error
        return null;
    }


    /// <summary>
    /// エラー発生時に次回　参照用のavsファイル作成
    /// </summary>
    /// <remarks>
    /// ・作成済みのavsファイルはあらかじめ削除しておくこと。
    /// ・TrimFrame数を進めないために　previous endframe, previous endframeにする。
    /// 
    /// 　TsShortName.p2.d2v_1000__2000.avs　を削除
    /// 　TsShortName.p2.d2v_1000__1000.avs　を作成
    /// 
    /// </remarks>
    public static void CreateDummyAvs_OnError()
    {
      int[] trimFrame_prv1;
      {
        if (PathList.PartALL || PathList.PartNo == 1)
        {
          trimFrame_prv1 = new int[] { 0, 0 };
        }
        else
        {
          trimFrame_prv1 = GetTrimFrame_fromAvsName(PathList.WorkName_prv1 + ".*_*__*.avs");
        }
      }

      string avsPath = string.Format(
        "{0}.{1}_{2}__{3}.avs",
        PathList.WorkPath,
        PathList.Avs_iPlugin.ToString(),
        trimFrame_prv1[1],
        trimFrame_prv1[1]);

      File.Create(avsPath).Close();

    }

    #endregion GetTrimFrame_fromName






    #region CalcTrimFrame

    /// <summary>
    /// トリム用フレーム計算
    /// </summary>
    /// <param name="totalframe">総フレーム数</param>
    /// <param name="trimFrame_prv1">前回のトリムフレーム数</param>
    /// <returns>開始、終了フレーム数</returns>
    public static int[] CalcTrimFrame(int totalframe, int[] trimFrame_prv1)
    {
      int beginFrame = 0, endFrame = 0;

      if (PathList.PartNo == 1)
      {
        beginFrame = 0;
        endFrame = totalframe - 1;
      }
      else if (2 <= PathList.PartNo)
      {
        if (trimFrame_prv1 == null)
          throw new LGLException("previous trim frame is null");

        beginFrame = trimFrame_prv1[1] + 1;                  //前回の終端フレーム数＋１
        endFrame = totalframe - 1;
      }
      else if (PathList.PartALL)
      {
        beginFrame = 0;
        endFrame = totalframe - 1;
      }

      return new int[] { beginFrame, endFrame };
    }

    #endregion GetTrimFrame


    #region CreateTrimAvs

    /// <summary>
    /// トリムつきAVS作成  共通部
    /// </summary>
    /// <param name="trimFrame">トリム開始、終了フレーム数</param>
    /// <returns>作成したavsパス</returns
    public static List<string> CreateTrimAvs(int[] trimFrame)
    {
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];

      //リソース読込み
      var avsText = FileR.ReadFromResource("LGLauncher.ResourceText.BaseTrimAvs.avs");

      //AVS書き換え
      for (int i = 0; i < avsText.Count; i++)
      {
        var line = avsText[i];

        //current dir
        line = Regex.Replace(line, "#AvsWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);

        //Detector
        if (PathList.Detector == LogoDetector.Join_Logo_Scp)
        {
          line = Regex.Replace(line, "#Join_Logo_Scp#", "", RegexOptions.IgnoreCase);
        }
        else
        {
          line = Regex.Replace(line, "#LogoGuillo#", "", RegexOptions.IgnoreCase);
        }

        //Trim
        if (1 <= PathList.PartNo)
        {
          line = Regex.Replace(line, "#EnableTrim#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#BeginFrame#", "" + beginFrame, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#EndFrame#", "" + endFrame, RegexOptions.IgnoreCase);
        }
        avsText[i] = line;
      }

      return avsText;

    }

    #endregion CreateTrimAvs

  }
}