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
  using OctNov.IO;

  public static class JLS
  {
    /// <summary>
    ///  JLSの出力 (avs Trim(0,1)) をLogoGuilloと同じ形式にする。
    ///    JLS  .p1.jls.result.txt　→　List<int>　→　.p1.frame.txt
    /// </summary>
    public static List<int> Result_to_Frame(bool islast_jls)
    {
      string avsTrimPath, framePath;
      {
        if (islast_jls == false)
        {
          avsTrimPath = PathList.WorkPath + ".jls.txt";
          framePath = PathList.WorkPath + ".frame.txt";
        }
        else
        {
          // last
          avsTrimPath = Path.Combine(PathList.LWorkDir,
                                     PathList.TsShortName + ".jls.last.txt");
          framePath = Path.Combine(PathList.LWorkDir,
                                   PathList.TsShortName + ".jls.last.frame.txt");
        }
      }
      if (File.Exists(avsTrimPath) == false)
        return null;

      //読
      List<int> framelist;
      {
        var readfile = TextR.ReadAllLines(avsTrimPath);
        if (readfile == null) return null;

        //１行に変換   List<string>  -->  string
        string liner = "";
        readfile.ForEach((line) => { liner += line; });
        framelist = ConvertFrame.AvsTrim_to_FrameList(liner);
      }

      //List<int>  -->  List<string>
      var frameText = framelist.Select(f => f.ToString()).ToList();
      //書
      File.WriteAllLines(framePath, frameText, TextEnc.Shift_JIS);

      return framelist;
    }


  }
}