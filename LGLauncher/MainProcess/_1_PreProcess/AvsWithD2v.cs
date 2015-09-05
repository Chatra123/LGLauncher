using System.IO;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  internal class AvsWithD2v : AbstractAvsMaker
  {
    public override string AvsPath { get; protected set; }            //作成したAVSのパス
    public override int[] TrimFrame { get; protected set; }           //今回のトリム用フレーム数
    public override int[] TrimFrame_m1 { get; protected set; }        //前回のトリム用フレーム数

    /// <summary>
    /// Trim付きavs作成
    /// </summary>
    /// <returns>作成したd2vパス</returns>
    public override void Make()
    {
      //ファイルチェック
      if (File.Exists(PathList.D2vPath) == false) throw new LGLException("D2vPath not exist");

      //フォーマットを整える
      string formatD2vPath = FormatD2v();

      //フレーム数取得用のavs作成
      string infoAvsPath = CreateInfoAvs_d2v(formatD2vPath);

      //avs実行
      MakeAvsCommon.RunInfoAvs(infoAvsPath);

      //総フレーム数取得
      var avsInfo = MakeAvsCommon.GetAvsInfo(PathList.WorkName + ".d2vinfo.txt");
      int totalframe = (int)avsInfo[0];

      //前回のトリム用フレーム数取得
      this.TrimFrame_m1 = (2 <= PathList.No)
                              ? MakeAvsCommon.GetTrimFrame_fromName(PathList.WorkName_m1 + ".d2v_*__*.avs")
                              : null;

      //トリム用フレーム数取得
      this.TrimFrame = MakeAvsCommon.GetTrimFrame(totalframe, TrimFrame_m1);

      //Trim付きavs作成
      this.AvsPath = CreateTrimAvs_d2v(formatD2vPath, TrimFrame);
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

      if (PathList.Mode_IsLast && isFinished == false)
        throw new LGLException("d2v format is not finished");

      //終端のフォーマットを整える
      var formatText = readText;
      if (1 <= PathList.No)
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
    /// <param name="d2vPath">avs内で読み込むd2vファイルパス</param>
    /// <param name="trimBeginEnd">トリムする開始、終了フレーム数</param>
    /// <returns>作成したavsパス</returns
    private string CreateTrimAvs_d2v(string d2vPath, int[] trimBeginEnd)
    {
      int beginFrame = trimBeginEnd[0];
      int endFrame = trimBeginEnd[1];

      //リソース読込み
      var avsText = FileR.ReadFromResource("LGLauncher.ResourceText.BaseTrimAvs.avs");

      //AVS書き換え
      string d2vName = Path.GetFileName(d2vPath);
      for (int i = 0; i < avsText.Count; i++)
      {
        var line = avsText[i];
        line = Regex.Replace(line, "#AvsWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#D2vName#", d2vName, RegexOptions.IgnoreCase);
        if (1 <= PathList.No)
        {
          line = Regex.Replace(line, "#EnableTrim#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#BeginFrame#", "" + beginFrame, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#EndFrame#", "" + endFrame, RegexOptions.IgnoreCase);
        }
        avsText[i] = line;
      }

      //長さチェック
      //　30frame以下だとlogoGuilloでavs2pipemodがエラーで落ちる。
      //　120frame以下ならno frame errorと表示されて終了する。
      //　150frame以上に設定する。
      int avslen = endFrame - beginFrame;

      //5sec以上か？
      if (150 <= avslen)
      {
        //avs書込み
        string outAvsPath = PathList.WorkPath + ".d2v_" + beginFrame + "__" + endFrame + ".avs";
        File.WriteAllLines(outAvsPath, avsText, TextEnc.Shift_JIS);

        return outAvsPath;
      }
      else
      {
        //ビデオの長さが短い
        //　次回処理のGetTrimFrame()のために*.avsを作成しておく。
        //　ここが処理される可能性はないと思う。
        string outAvsPath = PathList.WorkPath + ".d2v_" + beginFrame + "__" + beginFrame + ".avs";
        File.WriteAllLines(outAvsPath, avsText, TextEnc.Shift_JIS);

        throw new LGLException("short video length");
      }
    }

    #endregion CreateTrimAvs_d2v
  }
}