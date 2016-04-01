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

  public static class Convert_JLS
  {
    /// <summary>
    ///  JLS  result.txt　→　frame.txt
    /// </summary>
    public static List<int> ResultAvs_to_FrameFile(bool use_LastCatText)
    {
      //入力、出力パス
      string avsPath, framePath;
      {
        if (use_LastCatText == false)
        {
          avsPath = PathList.WorkPath + ".jls.result.txt";
          framePath = PathList.WorkPath + ".frame.txt";
        }
        else
        {
          // jls lastcat text
          avsPath = Path.Combine(PathList.LWorkDir,
                                 PathList.TsShortName + ".jls.lastcat.result.txt");
          framePath = Path.Combine(PathList.LWorkDir,
                                   PathList.TsShortName + ".lastcat.frame.txt");
        }
      }

      if (File.Exists(avsPath) == false)
        return null;

      //読
      //avsファイルが無ければ　avsText= null、 framelist = nullになる。
      var avsText = FileR.ReadAllLines(avsPath);

      //１行に変換
      string avsliner = "";
      avsText.ForEach((line) => { avsliner += line; });


      var framelist = ConvertFrame.AvsTrim_to_FrameList(avsliner);

      if (framelist != null)
      {
        //List<int>  -->  List<string>
        var frameText = framelist.Select(f => f.ToString()).ToList();

        //書
        File.WriteAllLines(framePath, frameText, TextEnc.Shift_JIS);
      }

      return framelist;
    }










  }

}