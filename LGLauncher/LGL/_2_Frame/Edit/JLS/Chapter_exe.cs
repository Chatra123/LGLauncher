﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;


namespace LGLauncher.Frame.JLS
{
  using OctNov.IO;

  public static class Chapter_exe
  {
    /// <summary>
    ///  chapter_exeのscposを連結
    /// </summary>
    public static void Concat(int[] trimFrame)
    {
      //chapter_exeによって作成されるファイル              *.p3.jls.scpos.txt
      string add_ScposPath = PathList.WorkPath + ".jls.scpos.txt";

      //結合SCPos                                          *.jls.scpos.cat.txt
      string catPath = Path.Combine(PathList.LWorkDir,
                                    PathList.TsShortName + ".jls.scpos.cat.txt");

      //読
      List<string> add_ScposText = null, old_CatText = null;
      {
        add_ScposText = TextR.ReadAllLines(add_ScposPath);    // from  *.p3.jls.scpos.txt

        //前回までの結合フレームを取得
        if (2 <= PathList.PartNo)
        {
          old_CatText = TextR.ReadAllLines(catPath);          // from  *.jls.scpos.cat.txt
          if (old_CatText == null && add_ScposText == null)
            throw new LGLException("not detect previous scpos file");
        }

        //最終行の # SCPos:1000 2000 は除去
        add_ScposText = add_ScposText ?? new List<string>();
        add_ScposText = add_ScposText.Where(line => line.Trim().IndexOf("#") != 0)
                                     .Where(line => string.IsNullOrWhiteSpace(line) == false)
                                     .ToList();
        old_CatText = old_CatText ?? new List<string>();
        old_CatText = old_CatText.Where(line => line.Trim().IndexOf("#") != 0)
                                 .Where(line => string.IsNullOrWhiteSpace(line) == false)
                                 .ToList();
      }

      //連結 with offset
      //　　add_ScposTextがあれば連結、なければold_CatTextのまま
      List<string> new_CatText;
      {
        new_CatText = new List<string>(old_CatText);

        if (trimFrame == null)
          throw new LGLException("trimFrame is null");

        int beginFrame = trimFrame[0];
        int endFrame = trimFrame[1];

        if (PathList.IsPart)
        {
          //CHAPTER03  の 03 部分のオフセット数
          int chapcnt_offset = old_CatText.Count / 2;
          add_ScposText = AppendOffset_Scpos(add_ScposText, chapcnt_offset, beginFrame);
        }

        new_CatText.AddRange(add_ScposText);
        new_CatText.Add("# SCPos:" + endFrame + " " + endFrame);
        //簡略化のため連結部の繋ぎ目はそのまま
      }

      //書
      //次回の参照用
      File.WriteAllLines(catPath, new_CatText, TextEnc.Shift_JIS);
      //デバッグ用のコピー
      string catPath_part = PathList.WorkPath + ".jls.scpos.cat.txt";
      File.WriteAllLines(catPath_part, new_CatText, TextEnc.Shift_JIS);
    }


    /// <summary>
    /// Scposテキストをオフセット分ずらす
    /// </summary>
    static List<string> AppendOffset_Scpos(List<string> scposText, int chapcnt_offset, int frame_offset)
    {
      var newText = new List<string>();
      for (int i = 0; i < scposText.Count; i += 2)
      {
        string timecode_line = scposText[i];
        string name_line = scposText[i + 1];
        var new_lines = ApeendOffset_ScposElement(
                                                   timecode_line, name_line,
                                                   chapcnt_offset, frame_offset
                                                  );
        newText.AddRange(new_lines);
      }
      return newText;
    }


    /// <summary>
    /// ”Scposの２行”をオフセットだけずらす
    /// </summary>
    static List<string> ApeendOffset_ScposElement(string timecode_line, string name_line, int chapcnt_offset, int frame_offset)
    {
      /* Scpos sample
       * 
       * CHAPTER01=00:00:36.303                            <--  timecode_line
       * CHAPTER01NAME=28フレーム  SCPos:1112 1111         <--  name_line
       * CHAPTER02=00:01:08.602
       * CHAPTER02NAME=31フレーム ★★ SCPos:2081 2080
       */

      //文字抽出
      Match match_timecode, match_name;
      {
        const string timecode_pattern = @"CHAPTER(?<ChapCnt>\d+)=(?<Hour>\d+):(?<Min>\d+):(?<Sec>\d+).(?<MSec>\d+)";
        const string name_pattern = @"CHAPTER(\d+)Name=(?<Mute_Mark>.*)SCPos:(?<SC_End>(\d+))\s+(?<SC_Begin>(\d+))";
        match_timecode = new Regex(timecode_pattern, RegexOptions.IgnoreCase).Match(timecode_line);
        match_name = new Regex(name_pattern, RegexOptions.IgnoreCase).Match(name_line);

        if (match_timecode.Success == false || match_name.Success == false)
          throw new LGLException("scpos text regex match error");
      }

      //文字列から抽出する値
      int chapCnt;
      TimeSpan timecode;
      string timetext;            //00:01:08.602
      string Mute_and_Mark;       //31フレーム ★★ 
      int SC_End, SC_Begin;

      //文字　→　数値
      try
      {
        chapCnt = int.Parse(match_timecode.Groups["ChapCnt"].Value);

        int hour = int.Parse(match_timecode.Groups["Hour"].Value);
        int min = int.Parse(match_timecode.Groups["Min"].Value);
        int sec = int.Parse(match_timecode.Groups["Sec"].Value);
        int msec = int.Parse(match_timecode.Groups["MSec"].Value);
        timecode = new TimeSpan(0, hour, min, sec, msec);

        Mute_and_Mark = match_name.Groups["Mute_Mark"].Value;
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

      chapCnt += chapcnt_offset;
      SC_End += frame_offset;
      SC_Begin += frame_offset;


      //update line
      string new_timecode_line = string.Format("Chapter{0:D2}={1}",
                                               chapCnt, timetext);
      string new_name_line = string.Format("Chapter{0:D2}Name={1}SCPos:{2} {3}",
                                           chapCnt, Mute_and_Mark, SC_End, SC_Begin);
      return new List<string> { new_timecode_line, new_name_line };
    }


  }











}