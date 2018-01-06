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

  public static class LogoFrame
  {
    /// <summary>
    ///  LogoFrameのresultを連結
    /// </summary>
    public static void Concat(int[] trimFrame)
    {
      //logoframeによって作成されるファイル                     *.p3.jls.logoframe.txt
      string addText_Path = PathList.WorkPath + ".jls.logoframe.txt";

      //前回までのファイル                                      *.jls.logoframe.cat.txt
      string catText_Path = Path.Combine(PathList.LWorkDir,
                                         PathList.TsShortName + ".jls.logoframe.cat.txt");

      //読
      List<string> addText = null, old_CatText = null;
      {
        addText = TextR.ReadAllLines(addText_Path);         // from  *.p3.jls.logoframe.txt

        //前回までの結合フレームを取得
        if (2 <= PathList.PartNo)
        {
          old_CatText = TextR.ReadAllLines(catText_Path);    // from  *.jls.logoframe.cat.txt
          if (old_CatText == null && addText == null)
            throw new LGLException("not detect logoframe file");
        }
        //空白行を除去
        addText = addText ?? new List<string>();
        addText = addText.Where(line => string.IsNullOrWhiteSpace(line) == false)
                                             .ToList();
        old_CatText = old_CatText ?? new List<string>();
        old_CatText = old_CatText.Where(line => string.IsNullOrWhiteSpace(line) == false)
                                 .ToList();
      }

      //連結 with offset
      //　　addTextがあれば連結、なければold_CatTextのまま
      List<string> new_CatText;
      {
        new_CatText = new List<string>(old_CatText);

        if (PathList.IsPart && trimFrame != null)
        {
          int beginFrame = trimFrame[0];
          addText = AppendOffset_logoframe(addText, beginFrame);
        }
        new_CatText.AddRange(addText);
        //簡略化のため連結部の繋ぎ目はそのまま
      }

      //書
      //次回の参照用に上書き
      File.WriteAllLines(catText_Path, new_CatText, TextEnc.Shift_JIS);
      //デバッグ用のコピー
      string catPath_part = PathList.WorkPath + ".jls.logoframe.cat.txt";
      File.WriteAllLines(catPath_part, new_CatText, TextEnc.Shift_JIS);
    }


    /// <summary>
    /// logoframeテキストをオフセットだけずらす
    /// </summary>
    static List<string> AppendOffset_logoframe(List<string> logoframeText, int frame_offset)
    {
      var newText = new List<string>();

      for (int i = 0; i < logoframeText.Count; i++)
      {
        string line = logoframeText[i];
        var new_lines = ApeendOffset_logoframe(line, frame_offset);
        newText.AddRange(new_lines);
      }
      return newText;
    }


    /// <summary>
    /// logoframe１行をオフセットだけずらす
    /// </summary>
    static List<string> ApeendOffset_logoframe(string line, int frame_offset)
    {
      /*
       * logoframe sample
       * 
       *   64 S 0 BTM     64     64
       * 2863 E 0 ALL   2837   2863
       */

      //文字抽出
      Match match;
      {
        const string pattern = @"\s*(?<frame_1>\d+)\s+(?<SorE>\w+)\s+(?<fade>\d+)\s+(?<interlace>\w+)\s+(?<frame_2>\d+)\s+(?<frame_3>\d+)\s*";
        match = new Regex(pattern, RegexOptions.IgnoreCase).Match(line);
        if (match.Success == false)
          throw new LGLException("logoframe text regex match error");
      }
      //文字列から抽出する値
      int frame_1, frame_2, frame_3;
      int fade;
      string SorE, interlace;
      //文字　→　数値
      try
      {
        frame_1 = int.Parse(match.Groups["frame_1"].Value);
        frame_2 = int.Parse(match.Groups["frame_2"].Value);
        frame_3 = int.Parse(match.Groups["frame_3"].Value);

        fade = int.Parse(match.Groups["fade"].Value);
        SorE = match.Groups["SorE"].Value;
        interlace = match.Groups["interlace"].Value;
      }
      catch
      {
        throw new LGLException("logoframe text parse error");
      }

      //add offset
      frame_1 += frame_offset;
      frame_2 += frame_offset;
      frame_3 += frame_offset;
      //update line
      string new_line = string.Format("{0,6} {1} {2} {3} {4,6} {5,6}",
                                      frame_1,
                                      SorE, fade, interlace,
                                      frame_2, frame_3
                                     );
      return new List<string> { new_line };
    }


  }





}