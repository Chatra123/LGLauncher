using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;


namespace LGLauncher
{
  abstract class AbstractAvsMaker
  {
    public abstract string AvsPath { get; protected set; }
    public abstract int[] TrimFrame { get; protected set; }           //EditFrameに渡す値
    public abstract int[] TrimFrame_m1 { get; protected set; }        //TimeShiftSrtに渡す値

    public abstract void Make();
  }



  static class MakeAvsCommon
  {

    #region RunInfoAvs
    /// <summary>
    /// InfoAvs実行
    /// </summary>
    /// <param name="avsPath">実行するavsパス</param>
    public static void RunInfoAvs(string avsPath)
    {
      if (File.Exists(PathList.AVS2X) == false)
        throw new LGLException();


      var psi = new ProcessStartInfo();
      psi.FileName = PathList.AVS2X;
      psi.Arguments = " -info \"" + avsPath + "\"";
      psi.CreateNoWindow = true;
      psi.UseShellExecute = false;
      var prc = new Process();
      prc.StartInfo = psi;

      //実行
      if (prc.Start() == false) throw new LGLException();
      prc.WaitForExit(20 * 1000);      //目測だと３秒程はかかる

    }
    #endregion





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
        throw new LGLException();


      //数値に変換
      double frame, fps, time;
      bool canParse = true;

      canParse &= double.TryParse(infoText[0], out frame);
      canParse &= double.TryParse(infoText[1], out fps);
      canParse &= double.TryParse(infoText[2], out time);
      if (canParse == false)
        throw new LGLException();

      return new double[] { frame, fps, time };
    }
    #endregion






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
        throw new LGLException();                          //無い or 多い


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
        bool canParse = true;

        canParse &= int.TryParse(sbegin, out ibegin);
        canParse &= int.TryParse(send, out iend);
        if (canParse == false)
          throw new LGLException();

        return new int[] { ibegin, iend };
      }
      else
        return null;
    }
    #endregion





    #region GetTrimFrame
    /// <summary>
    /// トリム用フレーム数取得
    /// </summary>
    /// <param name="totalframe">総フレーム数</param>
    /// <param name="prvframe_getfrom">前回のフレーム数を取得するファイル名。ワイルドカード指定可</param>
    /// <returns>トリム開始、終了フレーム数</returns>
    public static int[] GetTrimFrame(int totalframe, int[] trimFrame_m1)
    {
      int beginFrame = 0, endFrame = 0;

      if (PathList.No == 1)
      {
        beginFrame = 0;
        endFrame = totalframe - 1;
      }
      else if (2 <= PathList.No)
      {
        if (trimFrame_m1 == null)
          throw new LGLException();

        beginFrame = trimFrame_m1[1] + 1;                  //前回の終端フレーム数＋１
        endFrame = totalframe - 1;
      }
      else if (PathList.No == -1)
      {
        beginFrame = 0;
        endFrame = totalframe - 1;
      }

      return new int[] { beginFrame, endFrame };
    }
    #endregion







  }
}
