using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;


namespace LGLauncher.Frame
{

  static class EditFrame
  {
    /// <summary>
    /// 前回のフレームファイルと結合させる
    /// </summary>
    public static List<int> Concat(int[] trimRange)
    {
      /*
       * 処理の流れ
       *   Join_Logo_Scp
       *      *.p1.jls.result.txt  -->  *.p1.frame.txt  -->  List<int> frame  -->  List<int> concat
       *   LogoGuillo
       *                                *.p1.frame.txt  -->  List<int> frame  -->  List<int> concat
       *
       */

      //Join_Logo_Scp
      //    *.p1.jls.result.txt  -->  *.p1.frame.txt
      //  JLSの出力をLogoGuilloと同じ形式にする。
      if (PathList.IsJLS)
      {
        //     *.p1.jls.result.txt  -->  *.p1.frame.txt
        JLS.JLS.Result_to_Frame(false);
        //make *.cat 
        //  *.p1.jls.scpos.txt  -->  *.scpos.cat.txt
        JLS.Chapter_exe.Concat(trimRange);
        JLS.LogoFrame.Concat(trimRange);

        //Re-execute JLS with *.cat
        if (PathList.IsLastPart)
        {
          var jl_cmd = PathList.JL_Cmd_Standard;
          var batPath = Bat.Bat_JLS.Make_AtLast(jl_cmd);
          Bat.BatLauncher.Launch(batPath);
          List<int> jls_last_frame = JLS.JLS.Result_to_Frame(true);
          return jls_last_frame;
        }
      }

      //Join_Logo_Scp  &  LogoGuillo
      //    *.p1.frame.txt  -->  List<int> frame  -->  List<int> concat
      //前回のフレームファイルと結合させる
      List<int> concat = LogoGuillo.Concat(trimRange);
      return concat;
    }


    /// <summary>
    /// チャプターファイル出力
    /// </summary>
    public static void OutputChapter(List<int> rawFrame, int[] trimRange)
    {
      if (rawFrame == null)
      {
        Log.WriteLine("OutputChapter: rawFrame is null");
        return;
      }
      if (trimRange.Count() != 2)
      {
        Log.WriteLine("trimRange is invalid count.  Count() = " + trimRange.Count());
        return;
      }


      int endFrame = trimRange[1];

      //raw frame
      if (2 == PathList.Output_RawFrame
        || 1 <= PathList.Output_RawFrame && PathList.IsLastPart)
      {
        string path;
        {
          string name = PathList.IsLastPart
                             ? PathList.TsNameWithoutExt + ".rawframe.txt"
                             : PathList.TsNameWithoutExt + ".part.rawframe.txt";
          path = Path.Combine(PathList.ChapDir_Misc, name);
        }
        var text = MakeChapText.Make_Frame(rawFrame, endFrame);
        if (text != null)
          File.WriteAllText(path, text, TextEnc.Shift_JIS);
      }


      //短いＭａｉｎ、ＣＭを潰す
      List<int> editFrame = null;
      {
        editFrame = new List<int>(rawFrame);  //コピー
        editFrame = ConvertFrame.FlatOut_Main(editFrame, PathList.Regard_NsecMain_AsCM);
        editFrame = ConvertFrame.FlatOut_CM__(editFrame, PathList.Regard_NsecCM_AsMain);
        editFrame = ConvertFrame.FlatOut_Main(editFrame, PathList.Regard_NsecMain_AsCM);
        editFrame = ConvertFrame.FlatOut_CM__(editFrame, PathList.Regard_NsecCM_AsMain);
      }

      //frame
      if (2 == PathList.Output_Frame
        || 1 <= PathList.Output_Frame && PathList.IsLastPart)
      {
        string path;
        {
          string name = PathList.IsLastPart
                             ? PathList.TsNameWithoutExt + ".frame.txt"
                             : PathList.TsNameWithoutExt + ".part.frame.txt";
          path = Path.Combine(PathList.ChapDir_Misc, name);
        }
        var text = MakeChapText.Make_Frame(editFrame, endFrame);
        if (text != null)
          File.WriteAllText(path, text, TextEnc.Shift_JIS);
      }

      //Tvtp
      if (2 == PathList.Output_Tvtp
        || 1 <= PathList.Output_Tvtp && PathList.IsLastPart)
      {
        string path;
        {
          string name = PathList.TsNameWithoutExt + ".chapter";
          path = Path.Combine(PathList.ChapDir_Tvtp, name);
        }
        var text = MakeChapText.Make_Tvtp(editFrame, endFrame);
        if (text != null)
          File.WriteAllText(path, text, TextEnc.UTF8_bom);
      }

      //SCPos & logoframe
      if (PathList.IsJLS)
        if (1 == PathList.Output_Jls && PathList.IsLastPart)
        {
          //  *.scpos.cat.txt
          string src = Path.Combine(PathList.LWorkDir, PathList.TsShortName);
          string dst = Path.Combine(PathList.ChapDir_Misc, PathList.TsNameWithoutExt);
          try
          {
            File.Copy(src + ".jls.scpos.cat.txt", dst + ".scpos.txt", true);
            File.Copy(src + ".jls.logoframe.cat.txt", dst + ".logoframe.txt", true);
          }
          catch { Log.WriteLine("fail to copy cat file to ChapDir"); }
        }

    }
  }//class
}














