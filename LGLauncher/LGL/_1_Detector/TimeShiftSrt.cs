/*
 * 
 * ・作成途中の srtを読み込む。 
 * ・フォーマットを整えてsrtとして使用できる形にする。 
 * ・shiftSecだけずらしたsrtを作成。 
 *  
 * 
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  using OctNov.IO;

  static class TimeShiftSrt
  {
    /// <summary>
    /// TimeShiftSrtファイルを作成
    /// </summary>
    /// <returns>作成したsrtファイルのパス</returns>
    public static string Make(double shiftSec)
    {
      //rtは削除されている可能性がある。
      if (File.Exists(PathList.SrtPath) == false) return "";

      //IsAllならsrtファイルをコピーしてreturn
      if (PathList.IsAll)
      {
        string copyDstPath = Path.Combine(PathList.LWorkDir, PathList.SrtName);
        try
        {
          File.Copy(PathList.SrtPath, copyDstPath);
          return copyDstPath;
        }
        catch
        {
          Log.WriteLine("srt file copy error");
          return "";
        }
      }

      //読込み
      var srtText = FileR.ReadAllLines(PathList.SrtPath, TextEnc.UTF8_bom);
      if (srtText == null) throw new LGLException("srt read file error");
      else if (srtText.Count <= 3) return "";                                  //まだテキストが書き込まれてない

      //フォーマット
      //最後の時間行から下を切り捨てる
      int idx_LastTimeline = 0;
      for (int idx = srtText.Count - 1; 0 <= idx; idx--)
      {
        string line = srtText[idx];
        //00:00:16,216 --> 00:00:18,218
        if (Regex.IsMatch(line, @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d"))
        {
          idx_LastTimeline = idx;
          break;
        }
      }

      //確実に書き込まれている行数までを取り出す。
      int idx_lastValidLine = idx_LastTimeline - 2;
      if (idx_lastValidLine < 0) throw new LGLException("srt format error");   //srt形式でない or テキストが４行以下
      var formatText = (PathList.IsLastPart)
                            ? srtText : srtText.GetRange(0, idx_lastValidLine + 1);

      //タイムコードの開始を０秒からにする
      var shiftText = formatText;
      if (2 <= PathList.PartNo)
      {
        shiftText = Shift_SrtText(shiftText, shiftSec);
      }
      if (shiftText.Count == 0) return "";

      //書込み
      string dstPath = PathList.WorkPath + ".srt";
      File.WriteAllLines(dstPath, shiftText, TextEnc.UTF8_bom);
      return dstPath;
    }

    #region Shift_SrtText

    //76                                         BlockIndex
    //00:10:04,630 --> 00:10:07,500              1stTimecode
    //明日の日本列島､２つの低気圧に挟
    //まれて
    //                                           line_blockend       2ndタイムコードの2つ前
    //77
    //00:10:07,500 --> 00:10:09,769              line_2ndTimecode
    //日本海側では午前中を中心に
    //

    /// <summary>
    /// ０秒からに振りなおしたsrtテキスト作成
    /// </summary>
    /// <param name="srtText">元になるsrtテキスト</param>
    /// <param name="shift_sec">指定秒数だけ時間をスライド</param>
    /// <returns></returns>
    ///
    ///  １つ目のタイムコードを探す。
    ///  ２つ目のタイムコードを探す。
    ///  １つ目から２つ目手前までを書き込む。
    ///  繰り返し
    ///
    private static List<string> Shift_SrtText(List<string> srtText, double shift_sec)
    {
      var shiftText = new List<string>();
      int BlockIndex_shift = 1;

      //１つ目のタイムコードを探す
      for (int line1 = 1; line1 < srtText.Count; line1++)
      {
        //タイムコード？
        if (Regex.IsMatch(srtText[line1], @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d"))
        {
          //シフトして、０秒以上か？
          string shifted_timecode;
          bool canShift = Shift_Timecode(srtText[line1], shift_sec, out shifted_timecode);
          if (canShift == false) continue;                  //０秒以下  or  変換失敗　でスキップ

          //２つ目のタイムコードを探す
          int line_2ndTimecode = -1;
          for (int line2 = line1 + 1; line2 < srtText.Count; line2++)
          {
            if (Regex.IsMatch(srtText[line2], @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d"))
            {
              //見つかった
              line_2ndTimecode = line2;
              break;
            }
          }

          //１つ目から ２つ目手前(line_blockend) までを書き込む。
          int line_blockend;
          if (line_2ndTimecode != -1)                      //2ndTimecodeがある
            line_blockend = line_2ndTimecode - 2;          //  →　2ndタイムコードの2つ前
          else                                             //2ndTimecodeがない
            line_blockend = srtText.Count - 1;             //  →　テキストの最後

          //ブロックを取り出す
          shiftText.Add("" + BlockIndex_shift);            //インデックス
          shiftText.Add(shifted_timecode);                 //シフトタイムコード

          int blocksize = line_blockend - (line1 + 1) + 1;
          shiftText.AddRange(srtText.GetRange(line1 + 1, blocksize));
          BlockIndex_shift++;

          //検索行を進める
          line1 = line_blockend + 1;
        }
      }

      return shiftText;
    }

    //DateTimeコンストラクター用のダミー値、０以外の任意の値。
    private readonly static int year_ = DateTime.Now.Year,
                                month = DateTime.Now.Month,
                                day__ = DateTime.Now.Day;

    /// <summary>
    /// タイムコードをシフトした値が０秒以上か？
    /// </summary>
    /// <param name="timecode_Base">元になるタイムコード</param>
    /// <param name="shift_sec">マイナス方向へシフトする秒数</param>
    /// <param name="shifted_timecode">変換後のタイムコード</param>
    /// <returns>正常に変換できたか</returns>
    /// <remarks>
    ///     timecode_Base     "00:10:04,630 --> 00:10:07,500"
    ///     shift_sec             02:05
    ///     timecode_Shift    "00:07:09,630 --> 00:08:02,500"    戻り値
    /// </remarks>
    private static bool Shift_Timecode(string timecode_Base, double shift_sec, out string shifted_timecode)
    {
      //型変換
      // string "00:10:04,630"  →  DateTime
      var TimecodeToDateTime = new Func<string, DateTime>(
        (timecode) =>
        {
          string sHour, sMin_, sSec_, sMsec;
          int iHour, iMin_, iSec_, iMsec;
          sHour = timecode.Substring(0, 2);
          sMin_ = timecode.Substring(3, 2);
          sSec_ = timecode.Substring(6, 2);
          sMsec = timecode.Substring(9, 3);

          //if (int.TryParse(sHour, out iHour) == false) return new DateTime();
          //if (int.TryParse(sMin_, out iMin_) == false) return new DateTime();
          //if (int.TryParse(sSec_, out iSec_) == false) return new DateTime();
          //if (int.TryParse(sMs__, out iMs__) == false) return new DateTime();
          try
          {
            iHour = int.Parse(sHour);
            iMin_ = int.Parse(sMin_);
            iSec_ = int.Parse(sSec_);
            iMsec = int.Parse(sMsec);
            return new DateTime(year_, month, day__, iHour, iMin_, iSec_, iMsec);
          }
          catch
          {
            return new DateTime();
          }
        });

      shifted_timecode = "";

      //                                                                12345678901234567890123456789    
      if (timecode_Base.Length < 29) return false;                   //"00:10:04,630 --> 00:10:07,500"  is 29char
      string timecode_Begin = timecode_Base.Substring(0, 12);        //00:10:04,630
      string timecode_End__ = timecode_Base.Substring(17, 12);       //00:10:07,500

      var timeBegin = TimecodeToDateTime(timecode_Begin);
      var timeEnd__ = TimecodeToDateTime(timecode_End__);
      if (timeBegin == new DateTime()
            || timeEnd__ == new DateTime()) return false;            //変換失敗

      var timeBegin_shift = timeBegin.AddSeconds(-1 * shift_sec);
      var timeEnd___shift = timeEnd__.AddSeconds(-1 * shift_sec);

      var timeZero = new DateTime(year_, month, day__, 0, 0, 0, 0);
      var spanBegin = (timeBegin_shift - timeZero).TotalSeconds;     //０ to timeB_shiftまでのスパン
      var spanEnd__ = (timeEnd___shift - timeZero).TotalSeconds;     //    マイナスなら00:00:00より前


      //０秒以上ならtimecode_Shiftを作成
      if (spanBegin <= 0 && 0 < spanEnd__)
      {
        //開始時間が00:00:00以下
        shifted_timecode = "00:00:00,000"
                          + " --> "
                          + timeEnd___shift.ToString("HH:mm:ss,fff");
        return true;
      }
      else if (0 < spanBegin && 0 < spanEnd__)
      {
        //両方00:00:00より大きい
        shifted_timecode = timeBegin_shift.ToString("HH:mm:ss,fff")
                          + " --> "
                          + timeEnd___shift.ToString("HH:mm:ss,fff");
        return true;
      }
      else
      {
        //両方00:00:00以下
        return false;
      }
    }

    #endregion Shift_SrtText

  }//class
}