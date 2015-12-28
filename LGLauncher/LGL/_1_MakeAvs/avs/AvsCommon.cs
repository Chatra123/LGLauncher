using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace LGLauncher
{
  using OctNov.IO;

  internal abstract class AbstractAvsMaker
  {

    public abstract string AvsPath { get; protected set; }            //作成したAVSのパス
    public abstract int[] TrimFrame { get; protected set; }           //今回のトリム用フレーム数
    public abstract int[] TrimFrame_m1 { get; protected set; }        //前回のトリム用フレーム数  minus 1

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

      prc.WaitForExit(120 * 1000);      //３秒程はかかる。
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
          break;                                           //ファイル取得成功
      }

      if (infoText == null || infoText.Count < 4)
        throw new LGLException("avsinfo is invalid");

      //数値に変換
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


    #region GetTrimFrame_fromName

    /// <summary>
    /// ファイル名から開始、終了フレーム数取得
    /// </summary>
    /// <param name="nameKey">対象のファイル名。ワイルドカード指定可</param>
    /// <returns>開始、終了フレーム数</returns>
    /// <remarks>TimeShiftSrt、EditFrameからも取得される。</remarks>
    public static int[] GetTrimFrame_fromName(string nameKey)
    {
      //ファイル検索
      var files = Directory.GetFiles(PathList.LWorkDir, nameKey);
      if (files.Count() != 1)
        throw new LGLException("avs files.Count() != 1. could'nt specify previous trim range.");                          // 0 or 多い

      //正規表現パターン
      //TsShortName.p1.d2v_0__2736.avs
      //TsShortName.p1.lwi_0__2736.avs
      //  <begin>      0
      //  <end>     2736
      var regex = new Regex(@".*\.\w+_(?<begin>\d+)__(?<end>\d+)\.avs", RegexOptions.IgnoreCase);

      Match match = regex.Match(files[0]);

      //成功  数値に変換
      if (match.Success)
      {
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
          throw new LGLException("filename parse error. ccould'nt specify previous trim range.");
        }
      }
      else
        return null;
    }

    #endregion GetTrimFrame_fromName


    #region GetTrimFrame

    /// <summary>
    /// トリム用フレーム数取得
    /// </summary>
    /// <param name="totalframe">総フレーム数</param>
    /// <param name="trimFrame_m1">前回のフレーム数を取得するファイル名。ワイルドカード指定可</param>
    /// <returns>トリム開始、終了フレーム数</returns>
    public static int[] GetTrimFrame(int totalframe, int[] trimFrame_m1)
    {
      int beginFrame = 0, endFrame = 0;

      if (PathList.PartNo == 1)
      {
        beginFrame = 0;
        endFrame = totalframe - 1;
      }
      else if (2 <= PathList.PartNo)
      {
        if (trimFrame_m1 == null)
          throw new LGLException("previous trim frame is null");

        beginFrame = trimFrame_m1[1] + 1;                  //前回の終端フレーム数＋１
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
    /// <param name="trimBeginEnd">トリムする開始、終了フレーム数</param>
    /// <returns>作成したavsパス</returns
    public static List<string> CreateTrimAvs(int[] trimBeginEnd)
    {
      int beginFrame = trimBeginEnd[0];
      int endFrame = trimBeginEnd[1];

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