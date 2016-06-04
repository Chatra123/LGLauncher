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

  public static class JLS
  {
    /// <summary>
    ///  JLSの出力をLogoGuilloと同じ形式にする。
    ///    JLS  .p1.jls.result.txt　→　List<int>　→　.p1.frame.txt
    /// </summary>
    public static List<int> Result_to_Frame(bool islast_jls)
    {
      //パス
      string avsTrimPath, framePath;
      {
        if (islast_jls == false)
        {
          avsTrimPath = PathList.WorkPath + ".jls.result.txt";
          framePath = PathList.WorkPath + ".frame.txt";
        }
        else
        {
          // last
          avsTrimPath = Path.Combine(PathList.LWorkDir,
                                     PathList.TsShortName + ".jls.last.result.txt");
          framePath = Path.Combine(PathList.LWorkDir,
                                   PathList.TsShortName + ".jls.last.frame.txt");
        }
      }
      if (File.Exists(avsTrimPath) == false)
        return null;

      //読
      List<int> framelist;
      {
        var readfile = FileR.ReadAllLines(avsTrimPath);
        if (readfile == null) return null;

        //１行に変換   List<string>  -->  string
        string liner = "";
        readfile.ForEach((line) => { liner += line; });

        framelist = EditFrame.AvsTrim_to_FrameList(liner);
      }

      //書
      {
        //List<int>  -->  List<string>
        var frameText = framelist.Select(f => f.ToString()).ToList();
        File.WriteAllLines(framePath, frameText, TextEnc.Shift_JIS);
      }

      return framelist;
    }


  }
}