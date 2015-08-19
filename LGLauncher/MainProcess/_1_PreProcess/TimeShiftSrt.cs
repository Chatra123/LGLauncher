using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  internal static class TimeShiftSrt
  {
    /// <summary>
    /// TimeShiftSrtファイルを作成
    /// </summary>
    /// <returns>作成したsrtファイルのパス</returns>
    ///
    static public string Make(int[] trimFrame_m1)
    {
      //ファイルチェック
      //　srtは削除されている可能性がある。
      if (File.Exists(PathList.SrtPath) == false) return "";

      //  partNo == -1ならsrtファイルをコピーしてreturn
      if (PathList.No == -1)
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
      if (srtText == null) throw new LGLException();
      else if (srtText.Count <= 3) return "";                                  //まだテキストが書き込まれてない

      //
      //フォーマット
      //
      //最後の時間行を抽出
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
      if (idx_lastValidLine < 0) throw new LGLException();                      //srt形式でない or テキストが４行以下
      var formatText = (PathList.Mode_IsLast)
                            ? srtText : srtText.GetRange(0, idx_lastValidLine + 1);

      //
      //シフト
      //
      //時間をシフトさせて開始を０秒からにする
      var shiftText = formatText;

      if (2 <= PathList.No)
      {
        if (trimFrame_m1 == null
              && 2 <= trimFrame_m1.Count()) throw new LGLException();

        //前回までの総時間
        //   trimFrame_m1[0]  前回のTrim開始フレーム
        //   trimFrame_m1[1]　前回のTrim終了フレーム
        double shiftSec = 1.0 * trimFrame_m1[1] / 29.970;

        //シフト
        shiftText = Shift_SrtTime(shiftText, shiftSec);
      }

      if (shiftText.Count == 0) return "";

      //出力ファイル名
      string dstPath = PathList.WorkPath + ".srt";

      //ファイル書込み
      File.WriteAllLines(dstPath, shiftText, TextEnc.UTF8_bom);

      return dstPath;
    }

    #region ShiftTime_Srt

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
    private static List<string> Shift_SrtTime(List<string> srtText, double shift_sec)
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
          string timecode_shift;
          bool canShift = Shift_Timecode(srtText[line1], shift_sec, out timecode_shift);
          if (canShift == false) continue;                  //０秒以下になったのでとばす

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
          shiftText.Add(timecode_shift);                   //シフトタイムコード

          int blocksize = line_blockend - (line1 + 1) + 1;
          shiftText.AddRange(srtText.GetRange(line1 + 1, blocksize));
          BlockIndex_shift++;

          //検索行を進める
          line1 = line_blockend + 1;
        }
      }

      return shiftText;
    }

    //DateTimeコンストラクター用のダミー値、任意の値でいい
    private static readonly int year_ = DateTime.Now.Year,
                                month = DateTime.Now.Month,
                                day__ = DateTime.Now.Day;

    /// <summary>
    /// タイムコードをシフトした値が０秒以上か？
    /// </summary>
    /// <param name="timecode_Base">元になるタイムコード</param>
    /// <param name="shift_sec">マイナス方向へシフトする秒数</param>
    /// <param name="timecode_Shift">変換後のタイムコード</param>
    /// <returns>正常に変換できたか</returns>
    /// <remarks>
    ///     timecode_Base     "00:10:04,630 --> 00:10:07,500"
    ///     shift_sec             02:05
    ///     timecode_Shift    "00:07:09,630 --> 00:08:02,500"    戻り値
    /// </remarks>
    private static bool Shift_Timecode(string timecode_Base, double shift_sec, out string timecode_Shift)
    {
      //型変換
      // string "00:10:04,630"  →  DateTime
      var TimecodeToDateTime = new Func<string, DateTime>(
        (timecode) =>
        {
          string sHour, sMin_, sSec_, sMs__;
          int iHour, iMin_, iSec_, iMs__;
          sHour = timecode.Substring(0, 2);
          sMin_ = timecode.Substring(3, 2);
          sSec_ = timecode.Substring(6, 2);
          sMs__ = timecode.Substring(9, 3);

          if (int.TryParse(sHour, out iHour) == false) return new DateTime();
          if (int.TryParse(sMin_, out iMin_) == false) return new DateTime();
          if (int.TryParse(sSec_, out iSec_) == false) return new DateTime();
          if (int.TryParse(sMs__, out iMs__) == false) return new DateTime();

          return new DateTime(year_, month, day__, iHour, iMin_, iSec_, iMs__);
        });

      timecode_Shift = "";

      if (timecode_Base.Length < 29) return false;
      string timecode_Begin = timecode_Base.Substring(0, 12);        //00:10:04,630
      string timecode_End__ = timecode_Base.Substring(17, 12);       //00:10:07,500

      var timeBegin = TimecodeToDateTime(timecode_Begin);
      var timeEnd__ = TimecodeToDateTime(timecode_End__);
      if (timeBegin == new DateTime()
            || timeEnd__ == new DateTime()) return false;            //変換失敗

      var timeB_shift = timeBegin.AddSeconds(-1 * shift_sec);
      var timeE_shift = timeEnd__.AddSeconds(-1 * shift_sec);

      var timeZero = new DateTime(year_, month, day__, 0, 0, 0, 0);
      var spanB = (timeB_shift - timeZero).TotalSeconds;             //０ to timeB_shiftまでのスパン
      var spanE = (timeE_shift - timeZero).TotalSeconds;             //マイナスなら０秒前

      //０秒以上ならtimecode_Shiftを作成
      if (spanB <= 0 && 0 < spanE)
      {
        //開始時間が０以下
        timecode_Shift = "00:00:00,000"
                          + " --> "
                          + timeE_shift.ToString("HH:mm:ss,fff");
        return true;
      }
      else if (0 < spanB && 0 < spanE)
      {
        //両方０より大きい
        timecode_Shift = timeB_shift.ToString("HH:mm:ss,fff")
                          + " --> "
                          + timeE_shift.ToString("HH:mm:ss,fff");
        return true;
      }
      else
      {
        //両方０以下
        return false;
      }
    }

    #endregion ShiftTime_Srt
  }//class
}