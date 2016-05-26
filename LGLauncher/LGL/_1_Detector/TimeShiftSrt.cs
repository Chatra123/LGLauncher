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
      //srtはすでに削除されている可能性もある。
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

      //読
      var srtText = FileR.ReadAllLines(PathList.SrtPath, TextEnc.UTF8_bom);
      if (srtText == null) throw new LGLException("srt read file error");
      else if (srtText.Count <= 3) return "";                                  //まだテキストが書き込まれていない

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

      //書
      string dstPath = PathList.WorkPath + ".srt";
      File.WriteAllLines(dstPath, shiftText, TextEnc.UTF8_bom);
      return dstPath;
    }


    #region Shift_SrtText

    //
    //76                                         Srt_Index
    //00:10:04,630 --> 00:10:07,500              line_1stTimecode
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
    ///
    ///  １つ目のタイムコードを探す。
    ///  ２つ目のタイムコードを探す。
    ///  １つ目から２つ目手前までを書き込む。
    ///  繰り返し
    ///
    private static List<string> Shift_SrtText(List<string> srtText, double shift_sec)
    {
      var shiftText = new List<string>();
      int Srt_Index = 1;

      //１つ目のタイムコードを探す
      for (int i = 1; i < srtText.Count; i++)
      {
        //タイムコード？
        if (Regex.IsMatch(srtText[i], @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d"))
        {
          int line_1stTimecode = i;

          //タイムコードをシフトして、０秒以上か？
          string shift_timecode;
          bool canShift = Shift_Timecode(srtText[line_1stTimecode], shift_sec, out shift_timecode);
          if (canShift == false) continue;                  //０秒以下  or  変換失敗　でスキップ


          //２つ目のタイムコードを探す
          int line_2ndTimecode = -1;
          for (int line2 = line_1stTimecode + 1; line2 < srtText.Count; line2++)
          {
            if (Regex.IsMatch(srtText[line2], @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d"))
            {
              line_2ndTimecode = line2;
              break;
            }
          }

          int line_blockend = (line_2ndTimecode != -1)
            ? line_2ndTimecode - 2        // 2ndTimecodeがある  →　2ndタイムコードの2つ前
            : srtText.Count - 1;          //              ない  →　テキストの最後
          int blocksize = line_blockend - (line_1stTimecode + 1) + 1;

          //shiftText作成
          shiftText.Add("" + Srt_Index);
          shiftText.Add(shift_timecode);
          shiftText.AddRange(srtText.GetRange(i + 1, blocksize));

          //検索行を進める
          Srt_Index++;
          i = line_blockend + 1;
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
    /// <param name="base_timecode">元になるタイムコード</param>
    /// <param name="shift_sec">マイナス方向へシフトする秒数</param>
    /// <param name="shift_timecode">変換後のタイムコード</param>
    /// <returns>変換できたか</returns>
    /// <remarks>
    ///     base_timecode     "00:10:04,630 --> 00:10:07,500"
    ///     shift_sec             02:05
    ///     timecode_Shift    "00:07:09,630 --> 00:08:02,500"    戻り値
    /// </remarks>
    private static bool Shift_Timecode(string base_timecode, double shift_sec, out string shift_timecode)
    {
      //型変換
      // string "00:10:04,630"  →  DateTime
      var TimecodeToDateTime = new Func<string, DateTime>(
        (timecode) =>
        {
          string sHour = timecode.Substring(0, 2);
          string sMin_ = timecode.Substring(3, 2);
          string sSec_ = timecode.Substring(6, 2);
          string sMsec = timecode.Substring(9, 3);

          try
          {
            int iHour = int.Parse(sHour);
            int iMin_ = int.Parse(sMin_);
            int iSec_ = int.Parse(sSec_);
            int iMsec = int.Parse(sMsec);
            return new DateTime(year_, month, day__, iHour, iMin_, iSec_, iMsec);
          }
          catch
          {
            return new DateTime();
          }
        });

      shift_timecode = "";

      //                                                               12345678901234567890123456789    
      if (base_timecode.Length < 29) return false;                   //00:10:04,630 --> 00:10:07,500    29 chars
      string timecode_Begin = base_timecode.Substring(0, 12);        //00:10:04,630
      string timecode_End__ = base_timecode.Substring(17, 12);       //00:10:07,500

      var timeBegin = TimecodeToDateTime(timecode_Begin);
      var timeEnd__ = TimecodeToDateTime(timecode_End__);
      if (timeBegin == new DateTime()
            || timeEnd__ == new DateTime()) return false;            //変換失敗

      var timeBegin_shift = timeBegin.AddSeconds(-1 * shift_sec);
      var timeEnd___shift = timeEnd__.AddSeconds(-1 * shift_sec);

      var timeZero = new DateTime(year_, month, day__, 0, 0, 0, 0);
      var spanBegin = (timeBegin_shift - timeZero).TotalSeconds;     //０ to timeBegin_shiftまでのスパン
      var spanEnd__ = (timeEnd___shift - timeZero).TotalSeconds;     //    マイナスなら00:00:00より前


      //０秒以上ならshift_timecodeを作成
      if (spanBegin <= 0 && 0 < spanEnd__)
      {
        //開始時間が00:00:00以下
        shift_timecode = "00:00:00,000"
                          + " --> "
                          + timeEnd___shift.ToString("HH:mm:ss,fff");
        return true;
      }
      else if (0 < spanBegin && 0 < spanEnd__)
      {
        //両方00:00:00より大きい
        shift_timecode = timeBegin_shift.ToString("HH:mm:ss,fff")
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