using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;


namespace LGLauncher.Frame
{
  using OctNov.IO;

  /// <summary>
  /// フレームリストの編集、各形式への変換
  /// </summary>
  public static class ConvertFrame
  {
    /// <summary>
    /// read *.frame.txt  -->  List<int>
    /// </summary>
    /// <returns>
    /// 取得成功  -->  List<int>
    /// 　　失敗  -->  null
    /// </returns>
    public static List<int> Read_FrameFile(string framePath)
    {
      if (File.Exists(framePath) == false) return null;

      //読
      var text = FileR.ReadAllLines(framePath);
      if (text == null) return null;

      //コメント削除、トリム
      text = text.Select(
        (line) =>
        {
          int found = line.IndexOf("//");
          line = (0 <= found) ? line.Substring(0, found) : line;
          return line.Trim();
        })
        .Where((line) => string.IsNullOrWhiteSpace(line) == false)    //空白行削除
        .ToList();


      //List<string>  -->  List<int>
      List<int> frameList;
      try
      {
        frameList = text.Select(line => int.Parse(line)).ToList();
      }
      catch
      {
        frameList = null;  //変換失敗
      }

      //check
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;
      return frameList;
    }


    /// <summary>
    /// 短いＭａｉｎをつぶす
    /// </summary>
    /// <param name="frameList">元になるフレームリスト</param>
    /// <param name="mini_main_sec">指定秒数以下のＭａｉｎをつぶす</param>
    /// <returns>
    ///   成功  -->  List<int>
    ///   失敗  -->  null
    /// </returns>
    public static List<int> FlatOut_Main(List<int> frameList, double mini_main_sec)
    {
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;
      if (frameList.Count <= 2) return frameList;

      var newList = new List<int>();
      int mini_main = (int)(mini_main_sec * 29.970);// sec --> frame
      for (int i = 0; i < frameList.Count; i += 2)
      {
        double mainLen = frameList[i + 1] - frameList[i];
        if (mini_main < mainLen)
        {
          newList.Add(frameList[i]);
          newList.Add(frameList[i + 1]);
        }
      }
      return newList;
    }


    /// <summary>
    /// 短いＣＭをつぶす
    /// </summary>
    /// <param name="frameList">元になるフレームリスト</param>
    /// <param name="mini_cm_sec">指定秒数以下のＣＭをつぶす</param>
    /// <returns>
    ///   成功  -->  List<int>
    ///   失敗  -->  null
    /// </returns>
    /// <remarks>開始直後のＣＭはつぶさない。</remarks>
    public static List<int> FlatOut_CM__(List<int> frameList, double mini_cm_sec)
    {
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;
      if (frameList.Count <= 2) return frameList;

      //「frameList[i]」と「newListの末尾」の差が CM Length
      //
      // CM Lengthが長ければ newListに本編 frameList[i], frameList[i+1]を加える。
      //　　　　　　短ければ newList[last]を次の本編終端 frameList[i + 1]に置き換え短いＣＭをつぶす。
      //ただし、開始直後のＣＭはそのまま、
      //開始直後に短いＣＭがあっても本編には加えない。
      var newList = new List<int>();
      int mini_cm = (int)(mini_cm_sec * 29.970);// sec --> frame

      //1st main
      newList.Add(frameList[0]);
      newList.Add(frameList[1]);
      for (int i = 2; i < frameList.Count; i += 2)
      {
        int cmLen = frameList[i] - newList.Last();
        if (mini_cm < cmLen)
        {
          //長
          //ＣＭを採用し通常の　本編始端＆終端　を加える
          newList.Add(frameList[i]);
          newList.Add(frameList[i + 1]);
        }
        else
        {
          //短
          //ＣＭを無視し本編内とする。次の本編終端に置き換え短いＣＭをつぶす。
          newList[newList.Count - 1] = frameList[i + 1];
        }
      }

      return newList;
    }


    /// <summary>
    ///  avs Trim(1000,2000)  -->  List<int>
    /// </summary>
    /// <returns>
    ///   成功  -->  List<int>
    ///   失敗  -->  null
    /// </returns>
    public static List<int> AvsTrim_to_FrameList(string avsTrim)
    {
      if (avsTrim == null) return null;

      var frameList = new List<int>();
      foreach (Match m in Regex.Matches(avsTrim, @"Trim\((\d+),(\d+)\)"))
      {
        try
        {
          int begin = Int32.Parse(m.Groups[1].Value);
          int end = Int32.Parse(m.Groups[2].Value);
          frameList.Add(begin);
          frameList.Add(end);
        }
        catch
        {
          return null;
        }
      }

      return frameList;
    }


    /// <summary>
    /// List<int>  -->  Tvtpチャプター
    /// </summary>
    public static string To_TvtpChap(List<int> frameList)
    {
      //フレーム数を100msec単位の時間に変換
      //    300[frame]  -->  300 / 29.970 * 10  -->  100[100msec]
      var timeList = frameList.Select((frame) => { return (int)((1.0 * frame / 29.970) * 10); }).ToList();

      //intへの変換で丸められている。同じ値が続いたら＋１
      for (int i = 1; i < timeList.Count; i++)
      {
        if (timeList[i - 1] == timeList[i])
          timeList[i]++;
      }

      //convert
      string chapText = "c-";
      for (int i = 0; i < timeList.Count; i++)
      {
        int time = timeList[i];

        if (i == 0 && time != 0)
          chapText += "0dix-" + time + "dox-";            //開始直後のＣＭスキップ用
        else if (i % 2 == 0)
          chapText += "" + time + "dox-";                 //even    cm out
        else
          chapText += "" + time + "dix-";                 //odd     cm in
      }
      chapText += "0eox-c";

      return chapText;
    }
    /* TvtPlay  ChapterMap.cpp */
    // [チャプターコマンド仕様]
    // ・ファイルの文字コードはBOM付きUTF-8であること
    // ・Caseはできるだけ保存するが区別しない
    // ・"c-"で始めて"c"で終わる
    // ・チャプターごとに"{正整数}{接頭英文字}{文字列}-"を追加する
    //   ・{接頭英文字}が"c" "d" "e"以外のとき、そのチャプターを無視する
    //     ・"c"なら{正整数}の単位はmsec
    //     ・"d"なら{正整数}の単位は100msec
    //     ・"e"なら{正整数}はCHAPTER_POS_MAX(動画の末尾)
    //   ・{文字列}は0～CHAPTER_NAME_MAX-1文字
    // ・仕様を満たさないコマンドは(できるだけ)全体を無視する
    // ・例1: "c-c" (仕様を満たす最小コマンド)
    // ・例2: "c-1234cName1-3456c-2345c2ndName-0e-c"
    //
    //
    /* TvtPlay  Readme.txt */
    //TsSkipXChapter【ver.1.4～】
    //チャプタースキップする[=1]かどうか
    // スキップチャプター(名前が"ix"または"ox"で始まるもの)の間をスキップします。
    //


    /// <summary>
    /// List<int>  -->  Ogmチャプター type1
    /// </summary>
    public static string To_OgmChap_type1(List<int> frameList)
    {
      var timecodelist = Frame_to_TimeCode(frameList);
      var chapText = new StringBuilder();

      //convert
      for (int i = 0; i < frameList.Count; i++)
      {
        string timecode = timecodelist[i];
        string cnt = (i + 1).ToString("00");
        chapText.AppendLine(timecode + " " + "chapter " + cnt);
      }

      return chapText.ToString();
    }
    /*
     * Ogm Chapter  type1
     * 00:00:00.000 chapter 01
     * 00:00:01.935 chapter 02
     * 00:03:08.856 chapter 03
     * 00:10:00.000 chapter 04
     */


    /// <summary>
    /// List<int> --> Ogmチャプター type2
    /// </summary>
    public static string To_OgmChap_type2(List<int> frameList)
    {
      var timecodelist = Frame_to_TimeCode(frameList);
      var chapText = new StringBuilder();

      //convert
      for (int i = 0; i < frameList.Count; i++)
      {
        string timecode = timecodelist[i];
        string cnt = (i + 1).ToString("00");
        chapText.AppendLine("Chapter" + cnt + "=" + timecode);
        chapText.AppendLine("Chapter" + cnt + "Name=" + "chapter " + cnt);
      }

      return chapText.ToString();
    }
    /*
     * Ogm Chapter  type2
     * Chapter01=00:00:00.000
     * Chapter01Name=chapter 01
     * Chapter02=00:00:01.935
     * Chapter02Name=chapter 02
     * Chapter03=00:03:08.856
     * Chapter03Name=chapter 03
     */


    /// <summary>
    /// フレーム  -->  タイムコード文字列  00:10:20.345         for Ogmチャプター
    /// </summary>
    private static List<string> Frame_to_TimeCode(List<int> frameList)
    {
      // msec          <--  frame
      var msec = frameList.Select(frame => 1.0 * frame / 29.970 * 1000).ToList();
      // timespan      <--  msec
      var timespan = msec.Select(ms => new TimeSpan(0, 0, 0, 0, (int)ms)).ToList();
      // 00:10:20.345  <--  timespan
      var text = timespan.Select(tspan => new DateTime().Add(tspan).ToString("HH:mm:ss.fff")).ToList();
      return text;
    }










  }
}