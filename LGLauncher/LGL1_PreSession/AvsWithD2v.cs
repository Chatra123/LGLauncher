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
  class AvsWithD2v
  {
    //
    //Trim付きavs作成
    //
    static public string Make()
    {
      //ファイルチェック
      if (File.Exists(PathList.D2vPath) == false) { Log.WriteLine("AvsWithD2v  File.Exists(PathList.D2vPath"); return ""; }



      //フォーマットを整える
      string formatD2vPath = FormatD2v();
      if (File.Exists(formatD2vPath) == false) { Log.WriteLine("File.Exists(formatD2vPath) == false"); return ""; }


      //フレーム数取得用のavs作成
      string infoAvsPath = CreateInfoAvs_d2v(formatD2vPath);
      if (File.Exists(infoAvsPath) == false) { Log.WriteLine("File.Exists(infoAvsPath) == false"); return ""; }


      //avs実行
      bool success = AvsCommon.RunInfoAvs(infoAvsPath);
      if (success == false) { Log.WriteLine("RunInfoAvs(infoAvsPath)  success = false"); return ""; }


      //フレーム数取得
      var avsInfo = AvsCommon.GetAvsInfo(PathList.WorkName + ".d2vinfo.txt");
      if (avsInfo == null) { Log.WriteLine("avsInfo = null"); return ""; }
      int totalframe = (int)avsInfo[0];


      //トリム用フレーム数取得
      var trimBeginEnd = AvsCommon.GetTrimFrame(totalframe, PathList.WorkName_m1 + ".d2v_*__*.avs");
      if (trimBeginEnd == null) { Log.WriteLine("trimBeginEnd == null"); return ""; }


      //Trim付きavs作成
      string trimAvsPath = CreateTrimAvs_d2v(formatD2vPath, trimBeginEnd);


      return trimAvsPath;

    }



    //======================================
    //フォーマットを整える
    //　d2vファイルの簡易チェック
    //======================================
    #region FormatD2v
    static string FormatD2v()
    {
      //ファイル読込み
      var readText = FileR.ReadAllLines(PathList.D2vPath);
      if (readText == null) { Log.WriteLine("d2v read error"); return ""; }


      //d2vファイルの簡易チェック
      bool isMatch = true;
      if (readText.Count < 22) return "";                    //行数が少ない
      for (int i = 18; i < readText.Count - 3; i++)          //最終行は含めない
      {
        isMatch &= Regex.IsMatch(readText[i], @"\d+ \d+ \d+ \d+ \d+ \d+ \d+ .*");
      }
      if (isMatch == false) { Log.WriteLine("Is not d2v format"); return ""; }  //d2vファイルでない


      //FINISHEDがあるか？
      bool isFinished = false, haveFF = false;
      for (int idx = readText.Count - 1; readText.Count - 5 < idx; idx--)        //末尾から走査
      {
        string line = readText[idx];
        if (Regex.IsMatch(line, @"FINISHED.*", RegexOptions.IgnoreCase)) isFinished = true;
        if (Regex.IsMatch(line, @".*FF.*", RegexOptions.IgnoreCase)) haveFF = true;
      }
      isFinished &= haveFF;

      if (PathList.No == -1 && isFinished == false)
      {
        Log.WriteLine("Not found  FINISHED line:   PathList.PartNo == -1");
        return "";
      }


      //終端のフォーマットを整える
      var formatText = readText;
      if (1 <= PathList.No)
      {
        //ファイル終端を整える
        formatText = formatText.GetRange(0, formatText.Count - 1);              //最終行は含めない
        if (isFinished == false)
          formatText[formatText.Count - 1] += " ff";

        //動作に支障がないので、
        //　・Location=0,0,0,0は書き替えない
        //　・FINISHEDは書かない
      }


      //出力ファイル名
      string outPath = PathList.WorkPath + ".d2v";


      //ファイル書込み
      FileW.WriteAllLines(outPath, formatText);


      return outPath;
    }
    #endregion



    //======================================
    //フレーム数取得用のAVS作成
    //======================================
    #region CreateInfoAvs_d2v
    static string CreateInfoAvs_d2v(string partD2vPath)
    {
      //リソース読込み
      var avsText = FileR.ReadFromResource("baseGetInfo.avs");

      //avsパス作成
      string outAvsPath = PathList.WorkPath + ".d2vinfo.avs";

      //AVS書き換え
      string d2vName = Path.GetFileName(partD2vPath);
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
      FileW.WriteAllLines(outAvsPath, avsText);
      return outAvsPath;
    }
    /*
     * avs内のWriteFileStart()のファイル名が長いと*.d2vinfo.txtのファイル名が途中で切れる。
     * ファイルパスの長さが255byteあたりでファイル名が切れる
     */
    #endregion



    //======================================
    //トリムつきAVS作成
    //======================================
    #region CreateTrimAvs_d2v
    static string CreateTrimAvs_d2v(string formatD2vPath, int[] trimBeginEnd)
    {
      int beginFrame = trimBeginEnd[0];
      int endFrame = trimBeginEnd[1];

      //リソース読込み
      var avsText = FileR.ReadFromResource("baseTrimAvs.avs");

      //AVS書き換え
      string d2vName = Path.GetFileName(formatD2vPath);
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
        FileW.WriteAllLines(outAvsPath, avsText);
        return outAvsPath;
      }
      else
      {  //ビデオの長さが短い
        //　次回処理のGetTrimFrame()のために*.avsを作成しておく。
        //　ここが処理される可能性はないと思う。
        string outAvsPath = PathList.WorkPath + ".d2v_" + beginFrame + "__" + beginFrame + ".avs";
        FileW.WriteAllLines(outAvsPath, avsText);
        Log.WriteLine("short video length");
        return "";
      }
    }




    #endregion

  }


  //AvsWithD2v、AvsWithLwi共通部
  class AvsCommon
  {

    //======================================
    //InfoAvs実行
    //======================================
    #region RunInfoAvs
    public static bool RunInfoAvs(string avsPath)
    {
      if (File.Exists(PathList.AVS2X) == false) return false;

      var psi = new ProcessStartInfo();
      psi.FileName = PathList.AVS2X;
      psi.Arguments = " -info \"" + avsPath + "\"";
      psi.CreateNoWindow = true;
      psi.UseShellExecute = false;
      var prc = new Process();
      prc.StartInfo = psi;

      //実行
      if (prc.Start() == false) return false;
      prc.WaitForExit(20 * 1000);    //３秒程度はかかる
      return true;
    }
    #endregion



    //======================================
    //*.info.txtからフレーム数、時間を取得    
    //======================================
    #region GetAvsInfo
    public static double[] GetAvsInfo(string infoName)
    {
      //get file
      string infoPath = Path.Combine(PathList.LWorkDir, infoName);
      var infoText = new List<string>();
      for (int retry = 1; retry <= 10; retry++)//取得できるまで待機
      {
        if (File.Exists(infoPath) == false) { Thread.Sleep(2000); continue; };
        infoText = FileR.ReadAllLines(infoPath);
        if (infoText == null || infoText.Count < 4) { Thread.Sleep(2000); continue; }
        else { break; }  //ファイル取得成功
      }
      if (infoText == null || infoText.Count < 4) { return null; }


      //parse
      double result;
      //  frame
      if (double.TryParse(infoText[0], out result) == false) return null;
      double frame = result;
      //  fps
      if (double.TryParse(infoText[1], out result) == false) return null;
      double fps = result;
      //  time
      if (double.TryParse(infoText[2], out result) == false) return null;
      double time = result;


      return new double[] { frame, fps, time };
    }
    #endregion



    //======================================
    //ファイル名からフレーム数取得
    //    TimeShiftSrt、EditFrameからも取得される
    //======================================
    #region GetFrame_byName
    public static int[] GetFrame_byName(string nameKey)
    {
      //ファイル検索
      var files = Directory.GetFiles(PathList.LWorkDir, nameKey);
      if (files.Count() != 1) return null;    //見つからない or 多い


      //正規表現パターン
      //TsShortName.p1.d2v_0__2736.avs
      //TsShortName.p1.lwi_0__2736.avs
      var regex = new Regex(@".*\.\w+_(?<begin>\d+)__(?<end>\d+)\.avs", RegexOptions.IgnoreCase);
      //検索
      Match match = regex.Match(files[0]);


      //検索成功
      if (match.Success)
      {//数値に変換
        int ibegin, iend;
        string sbegin = match.Groups["begin"].Value;
        string send = match.Groups["end"].Value;
        if (int.TryParse(sbegin, out ibegin) == false) return null;  //パース失敗
        if (int.TryParse(send, out iend) == false) return null;
        return new int[] { ibegin, iend };
      }
      else
        return null;
    }
    #endregion



    //======================================
    //トリム用フレーム数取得
    //======================================
    #region GetTrimFrame
    public static int[] GetTrimFrame(int totalframe, string prvframe_getfrom)
    {
      int beginFrame = 0, endFrame = 0;

      if (PathList.No == 1)
      {
        beginFrame = 0;
        endFrame = totalframe - 1;
      }
      else if (2 <= PathList.No)  //前回の終端フレームから今回の開始フレーム決定
      {
        int[] frameset_m1 = AvsCommon.GetFrame_byName(prvframe_getfrom);    //前回のフレーム数取得
        if (frameset_m1 == null)
        { Log.WriteLine("fail to get previous frame set:" + prvframe_getfrom); return null; }  //取得失敗

        beginFrame = frameset_m1[1] + 1;                                    //前回の終端フレーム数＋１
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






