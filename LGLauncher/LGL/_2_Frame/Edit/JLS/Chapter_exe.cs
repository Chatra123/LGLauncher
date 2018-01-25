using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;


namespace LGLauncher.Frame.JLS
{

  public static class Chapter_exe
  {
    /// <summary>
    ///  chapter_exeのscposを連結
    /// </summary>
    public static void Concat(int[] trimRange)
    {
      //chapter_exeによって作成されるファイル                   *.p3.jls.scpos.txt
      string addText_Path = PathList.WorkPath + ".jls.scpos.txt";

      //前回までのファイル                                      *.jls.scpos.cat.txt
      string catText_Path = Path.Combine(PathList.LWorkDir,
                                         PathList.TsShortName + ".jls.scpos.cat.txt");

      //読
      List<string> addText = null, old_CatText = null;
      {
        addText = TextR.ReadAllLines(addText_Path);                // from  *.p3.jls.scpos.txt

        //前回までの結合フレームを取得
        if (2 <= PathList.PartNo)
        {
          old_CatText = TextR.ReadAllLines(catText_Path);          // from  *.jls.scpos.cat.txt
          if (old_CatText == null && addText == null)
            throw new LGLException("scpos file is not found");
        }
        //最終行の # SCPos:1000 2000 は除去
        addText = addText ?? new List<string>();
        addText = addText.Where(line => line.Trim().IndexOf("#") != 0)
                         .Where(line => string.IsNullOrWhiteSpace(line) == false)
                         .ToList();
        old_CatText = old_CatText ?? new List<string>();
        old_CatText = old_CatText.Where(line => line.Trim().IndexOf("#") != 0)
                                 .Where(line => string.IsNullOrWhiteSpace(line) == false)
                                 .ToList();
      }

      //連結 with offset
      //　　addTextがあれば連結、なければold_CatTextのまま
      List<string> new_CatText;
      {
        new_CatText = new List<string>(old_CatText);
        if (trimRange == null)
          throw new LGLException("trimRange is null");
        int beginFrame = trimRange[0];
        int endFrame = trimRange[1];
        //CHAPTER03  の 03 部分のオフセット数
        int chapcnt_offset = old_CatText.Count / 2;
        addText = AppendOffset_Scpos(addText, chapcnt_offset, beginFrame);

        new_CatText.AddRange(addText);
        new_CatText.Add("# SCPos:" + endFrame + " " + endFrame);
        //簡略化のため連結部の繋ぎ目はそのまま
      }

      //書
      //次回の参照用に上書き
      File.WriteAllLines(catText_Path, new_CatText, TextEnc.Shift_JIS);
      //デバッグ用のコピー
      string catPath_part = PathList.WorkPath + ".jls.scpos.cat.txt";
      File.WriteAllLines(catPath_part, new_CatText, TextEnc.Shift_JIS);
    }


    /// <summary>
    /// Scposをオフセット分ずらす
    /// </summary>
    static List<string> AppendOffset_Scpos(List<string> scposText, int chapCnt_offset, int frame_offset)
    {
      var newText = new List<string>();
      for (int i = 0; i < scposText.Count; i += 2)
      {
        string timecode_line = scposText[i];
        string name_line = scposText[i + 1];
        var new_lines = ApeendOffset_ScposElement(
                                                   timecode_line, name_line,
                                                   chapCnt_offset, frame_offset
                                                  );
        newText.AddRange(new_lines);
      }
      return newText;
    }


    /// <summary>
    /// ”Scposの２行”をオフセットだけずらす
    /// </summary>
    static List<string> ApeendOffset_ScposElement(string timecode_line, string name_line, int chapCnt_offset, int frame_offset)
    {
      /* Scpos sample
       * 
       * CHAPTER01=00:00:36.303                                timecode_line
       * CHAPTER01NAME=28フレーム  SCPos:1112 1111             name_line
       * CHAPTER02=00:01:08.602
       * CHAPTER02NAME=31フレーム ★★ SCPos:2081 2080
       */
      Match match_timecode, match_name;
      {
        const string timecode_pattern = @"CHAPTER(?<ChapCnt>\d+)=(?<Hour>\d+):(?<Min>\d+):(?<Sec>\d+).(?<MSec>\d+)";
        const string name_pattern = @"CHAPTER(\d+)Name=(?<Mute_Mark>.*)SCPos:(?<SC_End>(\d+))\s+(?<SC_Begin>(\d+))";
        match_timecode = new Regex(timecode_pattern, RegexOptions.IgnoreCase).Match(timecode_line);
        match_name = new Regex(name_pattern, RegexOptions.IgnoreCase).Match(name_line);

        if (match_timecode.Success == false || match_name.Success == false)
          throw new LGLException("scpos text regex match error");
      }

      //string --> int
      int chapCnt;
      TimeSpan timecode;
      string timetext;        //00:01:08.602
      string Mute_Mark;       //31フレーム ★★ 
      int SC_End, SC_Begin;
      try
      {
        chapCnt = int.Parse(match_timecode.Groups["ChapCnt"].Value);
        int hour = int.Parse(match_timecode.Groups["Hour"].Value);
        int min = int.Parse(match_timecode.Groups["Min"].Value);
        int sec = int.Parse(match_timecode.Groups["Sec"].Value);
        int msec = int.Parse(match_timecode.Groups["MSec"].Value);
        timecode = new TimeSpan(0, hour, min, sec, msec);
        Mute_Mark = match_name.Groups["Mute_Mark"].Value;
        SC_End = int.Parse(match_name.Groups["SC_End"].Value);
        SC_Begin = int.Parse(match_name.Groups["SC_Begin"].Value);
      }
      catch
      {
        throw new LGLException("scpos text parse error");
      }

      //add offset
      var msec_offset = 1.0 * frame_offset / 29.970 * 1000;
      timecode += new TimeSpan(0, 0, 0, 0, (int)msec_offset);
      timetext = new DateTime().Add(timecode).ToString("HH:mm:ss.fff");
      chapCnt += chapCnt_offset;
      SC_End += frame_offset;
      SC_Begin += frame_offset;

      //new line
      string new_timecode_line = string.Format("Chapter{0:D2}={1}",
                                               chapCnt, timetext);
      string new_name_line = string.Format("Chapter{0:D2}Name={1}SCPos:{2} {3}",
                                           chapCnt, Mute_Mark, SC_End, SC_Begin);
      return new List<string> { new_timecode_line, new_name_line };
    }


  }











}