using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;


namespace LGLauncher.EditFrame.JLS
{
  using OctNov.IO;

  public static class Concat_Scpos
  {

    /// <summary>
    ///  capter_exeのscposを連結
    /// </summary>
    public static void Concat(int[] trimFrame)
    {
      //avsの開始、終了フレーム番号
      //　オフセット用
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];


      //パス作成
      //chapter_exeによって作成されるファイル              *.p3.jls.scpos.txt
      string add_ScposPath = PathList.WorkPath + ".jls.scpos.txt";

      //前回までの結合SCPos                                *.jls.scpos.cat.txt
      string catPath = Path.Combine(PathList.LWorkDir,
                                    PathList.TsShortName + ".jls.scpos.cat.txt");


      //
      //テキスト読込み
      //
      List<string> old_CatText, add_ScposText;
      {
        add_ScposText = FileR.ReadAllLines(add_ScposPath);  // from  *.p3.jls.scpos.txt
        old_CatText = FileR.ReadAllLines(catPath);          // from  *.jls.scpos.cat.txt

        if (2 <= PathList.PartNo)
          if (old_CatText == null && add_ScposText == null)
            throw new LGLException("not detect scpos file");

        //最後の# SCPos:1000 2000 を除去
        old_CatText = old_CatText ?? new List<string>();
        old_CatText = old_CatText.Where(line => line.Trim().IndexOf("#") != 0)
                                 .Where(line => string.IsNullOrWhiteSpace(line) == false)
                                 .ToList();

        add_ScposText = add_ScposText ?? new List<string>();
        add_ScposText = add_ScposText.Where(line => line.Trim().IndexOf("#") != 0)
                                     .Where(line => string.IsNullOrWhiteSpace(line) == false)
                                     .ToList();
      }

      //
      //連結 with offset
      //　　add_ScposTextがあれば連結、なければold_CatTextのまま
      List<string> new_CatText = old_CatText;                                  // *.jls.scpos.cat.txt
      {
        if (1 <= PathList.PartNo
            && beginFrame != int.MaxValue)
        {
          //CHAPTER08  の 08 部分のオフセット数
          int chapcnt_offset = old_CatText.Count / 2;
          add_ScposText = ApeendOffset_Scpos(add_ScposText, chapcnt_offset, beginFrame);
        }

        //手間がかかるので連結部の繋ぎ目は消さない。
        new_CatText.AddRange(add_ScposText);
        new_CatText.Add("# SCPos:" + endFrame + " " + endFrame);
      }


      //
      //書込み
      //
      //次回の参照用
      File.WriteAllLines(catPath, new_CatText, TextEnc.Shift_JIS);

      //デバッグ記録用
      string catPath_part = PathList.WorkPath + ".jls.scpos.cat.txt";
      File.WriteAllLines(catPath_part, new_CatText, TextEnc.Shift_JIS);

    }


    /// <summary>
    /// Scposテキストをオフセット分ずらす    for JLS_Concat_Scpos
    /// </summary>
    static List<string> ApeendOffset_Scpos(List<string> scposText, int chapcnt_offset, int frame_offset)
    {
      var new_scposText = new List<string>();

      for (int i = 0; i < scposText.Count; i += 2)
      {
        string timecode_line = scposText[i];
        string name_line = scposText[i + 1];

        var new_lines = ApeendOffset_ScposElement(
                                                   timecode_line, name_line,
                                                   chapcnt_offset, frame_offset);
        new_scposText.AddRange(new_lines);
      }

      return new_scposText;
    }


    /// <summary>
    /// ”Scposの２行”をオフセットだけずらす    for JLS_Concat_Scpos
    /// </summary>
    static List<string> ApeendOffset_ScposElement(string timecode_line, string name_line, int chapcnt_offset, int frame_offset)
    {
      /*
      CHAPTER01=00:00:36.303
      CHAPTER01NAME=28フレーム  SCPos:1112 1111
      CHAPTER02=00:01:08.602
      CHAPTER02NAME=31フレーム ＿ SCPos:2081 2080
      */

      //Regexで数値抽出
      const string timecode_pattern = @"CHAPTER(?<ChapCnt>\d+)=(?<Hour>\d+):(?<Min>\d+):(?<Sec>\d+).(?<MSec>\d+)";
      const string name_pattern = @"CHAPTER(\d+)Name=(?<Mute>.*)SCPos:(?<SC_End>(\d+))\s+(?<SC_Begin>(\d+))";
      Match match_timecode = new Regex(timecode_pattern, RegexOptions.IgnoreCase).Match(timecode_line);
      Match match_name = new Regex(name_pattern, RegexOptions.IgnoreCase).Match(name_line);

      //変換失敗
      if (match_timecode.Success == false || match_name.Success == false)
        throw new LGLException("scpos text regex match error");


      //文字列から抽出する値
      int chapCnt;
      TimeSpan timecode;
      string timetext;

      string Mute;
      int SC_End, SC_Begin;

      //文字　→　数値
      {
        try
        {
          chapCnt = int.Parse(match_timecode.Groups["ChapCnt"].Value);

          int hour = int.Parse(match_timecode.Groups["Hour"].Value);
          int min = int.Parse(match_timecode.Groups["Min"].Value);
          int sec = int.Parse(match_timecode.Groups["Sec"].Value);
          int msec = int.Parse(match_timecode.Groups["MSec"].Value);
          timecode = new TimeSpan(0, hour, min, sec, msec);

          Mute = match_name.Groups["Mute"].Value;
          SC_End = int.Parse(match_name.Groups["SC_End"].Value);
          SC_Begin = int.Parse(match_name.Groups["SC_Begin"].Value);
        }
        catch
        {
          throw new LGLException("scpos text parse error");
        }
      }

      //add offset
      {
        var msec_offset = 1.0 * frame_offset / 29.970 * 1000;
        timecode += new TimeSpan(0, 0, 0, 0, (int)msec_offset);
        timetext = new DateTime().Add(timecode).ToString("HH:mm:ss.fff");

        chapCnt += chapcnt_offset;
        SC_End += frame_offset;
        SC_Begin += frame_offset;
      }

      //update line
      string new_timecode_line = string.Format("Chapter{0:D2}={1}", chapCnt, timetext);
      string new_name_line = string.Format("Chapter{0:D2}Name={1}SCPos:{2} {3}",
                                            chapCnt, Mute, SC_End, SC_Begin);

      return new List<string> { new_timecode_line, new_name_line };
    }


  }











}