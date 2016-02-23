using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace LGLauncher
{
  using OctNov.IO;

  internal class AvsWithD2v : AbstractAvsMaker
  {
    public override string AvsPath { get; protected set; }            //作成したAVSのパス
    public override int[] TrimFrame { get; protected set; }           //トリム用フレーム数

    /// <summary>
    /// Trim付きavs作成
    /// </summary>
    public override void Make()
    {
      //ファイルチェック
      //D2vPath
      if (File.Exists(PathList.D2vPath) == false)
        throw new LGLException("D2vPath not exist");

      //dll
      if (File.Exists(PathList.DGDecode_dll) == false)
        throw new LGLException("DGDecode.dll not exist");


      //Avs作成処理
      //フォーマットを整える
      string formatD2vPath = FormatD2v();

      //フレーム数取得用のavs作成
      string infoAvsPath = CreateInfoAvs_d2v(formatD2vPath);

      //avs実行
      MakeAvsCommon.RunInfoAvs(infoAvsPath);

      //総フレーム数取得
      var avsInfo = MakeAvsCommon.GetAvsInfo(PathList.WorkName + ".d2vinfo.txt");
      int totalframe = (int)avsInfo[0];

      //前回のトリム用フレーム数取得   previous 1
      int[] trimFrame_prv1 = (2 <= PathList.PartNo)
                                ? MakeAvsCommon.GetTrimFrame_fromAvsName(PathList.WorkName_prv1 + ".d2v_*__*.avs")
                                : null;

      //トリム用フレーム計算
      this.TrimFrame = MakeAvsCommon.CalcTrimFrame(totalframe, trimFrame_prv1);

      //Trim付きavs作成
      this.AvsPath = CreateTrimAvs_d2v(formatD2vPath, this.TrimFrame);

    }


    #region FormatD2v

    /// <summary>
    /// フォーマットを整える
    /// </summary>
    /// <returns>作成したd2vパス</returns>
    private string FormatD2v()
    {
      //ファイル読込み
      var readText = FileR.ReadAllLines(PathList.D2vPath);
      if (readText == null) throw new LGLException("d2v read error");

      //d2vファイルの簡易チェック
      bool isMatch = true;
      if (readText.Count < 22) return "";                                      //行数が少ない
      for (int i = 18; i < readText.Count - 3; i++)                            //最終行は含めない
      {
        isMatch &= Regex.IsMatch(readText[i], @"\d+ \d+ \d+ \d+ \d+ \d+ \d+ .*");
      }
      if (isMatch == false) throw new LGLException("d2v format error");        //d2vファイルでない


      //FINISHEDがあるか？
      bool isFinished = false, haveFF = false;
      for (int idx = readText.Count - 1; readText.Count - 5 < idx; idx--)      //末尾から走査
      {
        string line = readText[idx];
        if (Regex.IsMatch(line, @"FINISHED.*", RegexOptions.IgnoreCase)) isFinished = true;
        if (Regex.IsMatch(line, @".*FF.*", RegexOptions.IgnoreCase)) haveFF = true;
      }
      isFinished &= haveFF;

      //if (PathList.Mode_IsLast && isFinished == false)
      //  throw new LGLException("d2v format is not finished");


      //終端のフォーマットを整える
      //  isFinishedがなければ末尾は切り捨て
      var formatText = readText;
      if (isFinished == false)
      {
        //ファイル終端を整える
        formatText = formatText.GetRange(0, formatText.Count - 1);             //最終行は含めない
        if (isFinished == false)
          formatText[formatText.Count - 1] += " ff";

        //動作に支障がないので、
        //　・Location=0,0,0,0は書き替えない
        //　・FINISHEDは書かない
      }

      //ファイル書込み
      string outPath = PathList.WorkPath + ".d2v";
      File.WriteAllLines(outPath, formatText, TextEnc.Shift_JIS);

      return outPath;
    }

    #endregion FormatD2v


    #region CreateInfoAvs_d2v

    /// <summary>
    /// フレーム数取得用のAVS作成
    /// </summary>
    /// <param name="d2vPath">フレーム数取得対象のd2vパス</param>
    /// <returns>作成したavsパス</returns>
    private string CreateInfoAvs_d2v(string d2vPath)
    {
      //リソース読込み
      var avsText = FileR.ReadFromResource("LGLauncher.ResourceText.BaseGetInfo.avs");

      //AVS書き換え
      string d2vName = Path.GetFileName(d2vPath);

      for (int i = 0; i < avsText.Count; i++)
      {
        var line = avsText[i];

        line = Regex.Replace(line, "#AvsWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#InputPlugin#", PathList.DGDecode_dll, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#D2vName#", d2vName, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#InfoName#", PathList.WorkName + ".d2vinfo.txt", RegexOptions.IgnoreCase);

        avsText[i] = line;
      }

      //書込み
      string outAvsPath = PathList.WorkPath + ".d2vinfo.avs";
      File.WriteAllLines(outAvsPath, avsText, TextEnc.Shift_JIS);

      return outAvsPath;
    }

    /*
     * avs内のWriteFileStart()のファイル名が長いと*.d2vinfo.txtのファイル名が途中で切れる。
     * ファイルパスの長さが255byteあたりでファイル名が切れる
     */

    #endregion CreateInfoAvs_d2v


    #region CreateTrimAvs_d2v

    /// <summary>
    /// トリムつきAVS作成
    /// </summary>
    private string CreateTrimAvs_d2v(string d2vPath, int[] trimFrame)
    {
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];

      //トリムつきAVS作成
      //avs共通部分
      var avsText = MakeAvsCommon.CreateTrimAvs(trimFrame);

      //d2v部分
      string d2vName = Path.GetFileName(d2vPath);
      for (int i = 0; i < avsText.Count; i++)
      {
        var line = avsText[i];
        line = Regex.Replace(line, "#InputPlugin#", PathList.DGDecode_dll, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#D2vName#", d2vName, RegexOptions.IgnoreCase);

        avsText[i] = line;
      }

      //長さチェック
      //　 30frame以下だと logoGuilloの avs2pipemodがエラーで落ちる。
      //　120frame以下なら no frame errorと表示されて終了する。
      //　150frame以上に設定する。
      int avslen = endFrame - beginFrame;

      //5sec以上か？
      if (30 * 5 <= avslen)
      {
        //avs書込み
        string outAvsPath = PathList.WorkPath + ".d2v_" + beginFrame + "__" + endFrame + ".avs";
        File.WriteAllLines(outAvsPath, avsText, TextEnc.Shift_JIS);

        return outAvsPath;
      }
      else
      {
        throw new LGLException("short video length.  -lt 150 frame");
      }
    }

    #endregion CreateTrimAvs_d2v


  }
}