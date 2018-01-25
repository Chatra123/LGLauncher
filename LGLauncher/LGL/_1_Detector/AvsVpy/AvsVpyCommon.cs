using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;


namespace LGLauncher
{

  /// <summary>
  /// Avs or Vpy作成
  /// </summary>
  static class AvsVpyMaker
  {
    static bool IndexHasFormatted = false;
    static AbstractAvsMaker maker;

    /// <summary>
    /// AvsVpyMaker
    /// </summary>
    public static void Init()
    {
      //作成途中のd2v, lwiファイルを利用できるようにフォーマットを整える。
      if (IndexHasFormatted == false)
      {
        IndexHasFormatted = true;
        if (PathList.IsD2v)
          new D2vFile().Format();
        else
          new LwiFile().Format();
      }
      maker = PathList.IsAvs ? new AvsMaker() as AbstractAvsMaker :
              PathList.IsVpy ? new VpyMaker() as AbstractAvsMaker :
              null;
    }
    /// <summary>
    /// トリム範囲取得
    /// </summary>
    public static int[] GetTrimRange()
    {
      return maker.GetTrimRange();
    }
    /// <summary>
    /// avs作成
    /// </summary>
    public static string MakeScript(int[] trimRange)
    {
      return maker.MakeScript(trimRange);
    }
  }

  /// <summary>
  /// Avs, Vpyの抽象化
  /// </summary>
  abstract class AbstractAvsMaker
  {
    public abstract int[] GetTrimRange();
    public abstract string MakeScript(int[] trimRange);
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
      var info = new List<string>();

      for (int retry = 1; retry <= 3; retry++)
      {
        if (File.Exists(infoPath) == false)
        {
          Thread.Sleep(2000);
          continue;
        };

        info = TextR.ReadAllLines(infoPath);
        if (info == null || info.Count <= 1)
        {
          Thread.Sleep(500);
          continue;
        }
        else
          break;   //取得成功
      }
      if (info == null || info.Count <= 1)
        throw new LGLException("info file is invalid");

      try
      {
        double frame = double.Parse(info[0]);
        return new double[] { frame };
      }
      catch
      {
        throw new LGLException("frame count parse error");
      }
    }

    #endregion InfoScript



    #region GetTrimRange

    /// <summary>
    /// 今回のトリム範囲取得
    /// </summary>
    public static int[] GetTrimRange()
    {
      return GetTrimRange_fromName(PathList.WorkName + ".*__*" + PathList.AvsVpyExt);
    }

    /// <summary>
    /// 前回のトリム範囲取得
    /// </summary>
    public static int[] GetTrimRange_previous()
    {
      return GetTrimRange_fromName(PathList.WorkName_prv + ".*__*" + PathList.AvsVpyExt);
    }

    /// <summary>
    /// ファイル名からトリム範囲取得
    /// </summary>
    /// <param name="nameKey">対象のファイル名。ワイルドカード指定可</param>
    /// <returns>開始、終了フレーム数</returns>
    private static int[] GetTrimRange_fromName(string nameKey)
    {
      if (PathList.LWorkDir == null)
        return null;
      var files = Directory.GetFiles(PathList.LWorkDir, nameKey);
      if (files.Count() != 1)
      {
        Log.WriteLine("  Could'nt specify trim range." + "  files.Count() = " + files.Count());
        Log.WriteLine("    nameKey = " + nameKey);
        return null;
      }

      //TsShortName.p1.0__1000.avs
      //  <begin>      0
      //  <end>     1000
      var regex = new Regex(@".*\.(?<begin>\d+)__(?<end>\d+)\.[(avs)|(vpy)]", RegexOptions.IgnoreCase);
      Match match = regex.Match(files[0]);
      if (match.Success)
      {
        //string --> int
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
          return null; // parse error
        }
      }
      else
        return null;  // match error
    }

    #endregion GetTrimRange



    #region OutputScript

    /// <summary>
    /// Scriptファイル作成
    /// </summary>
    public static string OutputScript(int[] trimRange, List<string> scriptText,
                                      string ext, Encoding enc)
    {
      //長さチェック
      //   30frame以下だと LogoGuilloの avs2pipemodがエラーで落ちる。
      //  120frame以下なら no frame errorと表示されて終了する。
      //  150frame以上に設定。
      int beginFrame = trimRange[0];
      int endFrame = trimRange[1];
      int len = endFrame - beginFrame + 1;
      if (150 <= len)
      {
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
    /// ・trimRange数を進めないために　trimRange_prv[1], trimRange_prv[1]　にする。
    /// 
    /// 　TsShortName.p2.1001__2000.avs　を削除しておき
    /// 　TsShortName.p2.1000__1000.avs　を作成 
    /// </remarks>
    public static void CreateDummy_OnError()
    {
      int[] trimRange_prv;
      {
        if (PathList.Is1stPart)
        {
          trimRange_prv = new int[] { 0, 0 };
        }
        else
        {
          trimRange_prv = GetTrimRange_previous();
          if (trimRange_prv == null) return;
        }
      }

      string path = string.Format("{0}.{1}__{2}{3}",
                                  PathList.WorkPath,
                                  trimRange_prv[1],
                                  trimRange_prv[1],
                                  PathList.AvsVpyExt
                                  );
      File.Create(path).Close();
    }

    #endregion CreateDummy_OnError


  }
}