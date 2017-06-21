using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  using OctNov.IO;

  static class SrtFile
  {
    /*
     * ・作成途中の srtを読み込む。 
     * ・フォーマットを整えてsrtとして使用できる形にする。 
     * ・shiftSecだけずらしたsrtを作成。 
     */
    /// <summary>
    /// 時間調整したSrtファイルを作成
    /// </summary>
    /// <returns>作成したsrtファイルのパス</returns>
    public static string Format(double shiftSec)
    {
      //srtはすでに削除されている可能性もある。
      if (File.Exists(PathList.SrtPath) == false) return "";

      if (PathList.IsAll)
      {
        //コピーして終了
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
      var srtText = TextR.ReadAllLines(PathList.SrtPath, TextEnc.UTF8_bom);
      if (srtText == null)
        throw new LGLException("srt read file error");
      else if (srtText.Count <= 3)  //まだテキストが書き込まれていない
        return "";  

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
      if (idx_lastValidLine < 0)
        throw new LGLException("srt format error");   //srt形式でない or テキストが４行以下
      var formatText = PathList.IsLastProcess
        ? srtText : srtText.GetRange(0, idx_lastValidLine + 1);

      //タイムコードの開始を０秒からに振りなおす
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

    //srt sample
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
    /// ０秒からに振りなおしたsrtテキストを作成
    /// </summary>
    /// <param name="srtText">元になるsrtテキスト</param>
    /// <param name="shift_sec">指定秒数だけ時間をスライド</param>
    private static List<string> Shift_SrtText(List<string> srtText, double shift_sec)
    {
      const string Ptn = @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d";
      var shiftText = new List<string>();
      int Srt_Index = 1;

      for (int i = 1; i < srtText.Count; i++)
      {
        //１つ目のタイムコードを探す
        if (Regex.IsMatch(srtText[i], Ptn))
        {
          int line_1stTimecode = i;

          //タイムコードを調整
          string shift_timecode;
          bool canShift = Shift_Timecode(srtText[line_1stTimecode], shift_sec, out shift_timecode);
          if (canShift == false) continue;  //０秒以下 or 変換失敗でスキップ

          //２つ目のタイムコードを探す
          int line_2ndTimecode = -1;
          for (int line2 = line_1stTimecode + 1; line2 < srtText.Count; line2++)
          {
            if (Regex.IsMatch(srtText[line2], Ptn))
            {
              line_2ndTimecode = line2;
              break;
            }
          }

          //１つ目から２つ目手前までを書き込む
          int line_blockend = (line_2ndTimecode != -1)
            ? line_2ndTimecode - 2        // 2ndTimecodeがある  →　2ndタイムコードの2つ前まで
            : srtText.Count - 1;          //              ない  →　テキストの最後まで
          int blocksize = line_blockend - (line_1stTimecode + 1) + 1;
          shiftText.Add("" + Srt_Index);
          shiftText.Add(shift_timecode);
          shiftText.AddRange(srtText.GetRange(i + 1, blocksize));

          Srt_Index++;
          i = line_blockend + 1;
        }
      }
      return shiftText;
    }

    //DateTimeコンストラクター用のダミー値、０以外の任意の値。
    private static readonly int year_ = DateTime.Now.Year,
                                month = DateTime.Now.Month,
                                day__ = DateTime.Now.Day;
    /// <summary>
    /// タイムコードを調整する
    /// </summary>
    /// <param name="base_timecode">元になるタイムコード</param>
    /// <param name="shift_sec">マイナス方向へシフトする秒数、shift_secだけ戻す</param>
    /// <param name="shift_timecode">変換後のタイムコード</param>
    /// <returns>シフトした値が０秒以上か？</returns>
    /// <remarks>
    ///     base_timecode     "00:10:04,630 --> 00:10:07,500"
    ///     shift_sec             02:05
    ///     timecode_Shift    "00:07:09,630 --> 00:08:02,500"    戻り値
    /// </remarks>
    private static bool Shift_Timecode(string base_timecode, double shift_sec, out string shift_timecode)
    {
      // string "00:10:04,630"  →  DateTime
      var StringToDateTime = new Func<string, DateTime>(
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

      //戻り値
      shift_timecode = "";

      //                                                          12345678901234567890123456789    
      if (base_timecode.Length < 29) return false;              //00:10:04,630 --> 00:10:07,500    29 chars
      string str_Begin = base_timecode.Substring(0, 12);        //00:10:04,630
      string str_End__ = base_timecode.Substring(17, 12);       //00:10:07,500

      var time_Begin = StringToDateTime(str_Begin);
      var time_End__ = StringToDateTime(str_End__);
      if (time_Begin == new DateTime()
            || time_End__ == new DateTime()) return false;
      var time_Begin_shift = time_Begin.AddSeconds(-1 * shift_sec);
      var time_End___shift = time_End__.AddSeconds(-1 * shift_sec);

      var time_Zero = new DateTime(year_, month, day__, 0, 0, 0, 0);
      var span_Begin = (time_Begin_shift - time_Zero).TotalSeconds;  //０ to timeBegin_shiftまでのスパン
      var span_End__ = (time_End___shift - time_Zero).TotalSeconds;  //    マイナスなら00:00:00より前


      //０秒以上ならshift_timecodeを作成
      if (span_Begin <= 0 && 0 < span_End__)
      {
        //開始時間が00:00:00以下
        shift_timecode = "00:00:00,000"
                          + " --> "
                          + time_End___shift.ToString("HH:mm:ss,fff");
        return true;
      }
      else if (0 < span_Begin && 0 < span_End__)
      {
        //両方00:00:00より大きい
        shift_timecode = time_Begin_shift.ToString("HH:mm:ss,fff")
                          + " --> "
                          + time_End___shift.ToString("HH:mm:ss,fff");
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