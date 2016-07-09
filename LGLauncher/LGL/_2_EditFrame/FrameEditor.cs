using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;


namespace LGLauncher.EditFrame
{
  using OctNov.IO;
  /*
   * 変換の流れ
   * 
   *   static List<int> ConcatFrame(int[] trimFrame)
   *   {
   *     Join_Logo_Scp
   *         *.p1.jls.result.txt  -->  *.p1.frame.txt  -->  List<int> frame  -->  List<int> concat
   *         
   *     LogoGuillo
   *                                   *.p1.frame.txt  -->  List<int> frame  -->  List<int> concat
   *   }
   * 
   * 
   *   static void OutputChapter(List<int> rawFrame, int[] trimFrame)
   *   {
   *     List<int> concat  -->  chapter file
   *   }
   *   
   */
  static class FrameEditor
  {
    /// <summary>
    /// 結合フレームリストを作成
    /// </summary>
    public static List<int> ConcatFrame(int[] trimFrame)
    {
      //JLSの出力をLogoGuilloと同じ形式にする。
      //  Join_Logo_Scp
      //     *.p1.jls.result.txt  -->  *.p1.frame.txt
      if (PathList.Detector == DetectorType.Join_Logo_Scp)
      {
        JLS.JLS.Result_to_Frame(false);

        //Concat
        //  Chapter_exe, LogoFrame
        //    scpos.cat, logoframe.catはIsLastで使用する。
        if (PathList.IsPart)
        {
          JLS.Chapter_exe.Concat(trimFrame);
          JLS.LogoFrame.Concat(trimFrame);
        }

        //Re-execute JLS with *.cat and JL_Cmd_Standard
        //  Join_Logo_Scp
        if (PathList.IsPart && PathList.IsLastPart)
        {
          var jl_cmd = PathList.JL_Cmd_Standard;
          var batPath = Bat_Join_Logo_Scp.Make_AtLast(jl_cmd);
          BatLauncher.Launch(batPath);
          List<int> jls_last_frame = JLS.JLS.Result_to_Frame(true);
          return jls_last_frame;
        }
      }

      //フレームテキスト合成
      //  Join_Logo_Scp  &  LogoGuillo
      //    *.p1.frame.txt  -->  List<int> frame  -->  List<int> concat
      List<int> concat = LogoGuillo.Concat(trimFrame);
      return concat;
    }


    /// <summary>
    /// チャプター出力
    /// </summary>
    public static void OutputChapter(List<int> rawFrame, int[] trimFrame)
    {
      if (rawFrame == null)
      {
        Log.WriteLine("OutputChapter: rawFrame is null");
        return;
      }
      if (trimFrame.Count() != 2)
      {
        Log.WriteLine("trimFrame is invalid count.  Count() = " + trimFrame.Count());
        return;
      }


      int endFrame = trimFrame[1];

      //raw frame
      if ((PathList.IsPart && 2 <= PathList.Output_RawFrame)
        || (PathList.IsLastPart && 1 <= PathList.Output_RawFrame))
      {
        string path;
        {
          string dir = (Directory.Exists(PathList.DirPath_Misc))
                            ? PathList.DirPath_Misc
                            : PathList.TsDir;
          string name = (PathList.IsLastPart)
                             ? PathList.TsNameWithoutExt + ".rawframe.txt"
                             : PathList.TsNameWithoutExt + ".part.rawframe.txt";
          path = Path.Combine(dir, name);
        }

        ConvertToFile.To_FrameFile(path, rawFrame, endFrame);
      }


      //短いＭａｉｎ、ＣＭを潰す
      List<int> editFrame = null;
      {
        editFrame = new List<int>(rawFrame);
        editFrame = EditFrame.FlatOut_CM__(editFrame, PathList.Regard_NsecCM_AsMain);
        editFrame = EditFrame.FlatOut_Main(editFrame, PathList.Regard_NsecMain_AsCM);
        editFrame = EditFrame.FlatOut_CM__(editFrame, PathList.Regard_NsecCM_AsMain);
        editFrame = EditFrame.FlatOut_Main(editFrame, PathList.Regard_NsecMain_AsCM);
      }


      //frame
      if ((PathList.IsPart && 2 <= PathList.Output_Frame)
        || (PathList.IsLastPart && 1 <= PathList.Output_Frame))
      {
        string path;
        {
          string dir = (Directory.Exists(PathList.DirPath_Misc))
                            ? PathList.DirPath_Misc
                            : PathList.TsDir;
          string name = (PathList.IsLastPart)
                             ? PathList.TsNameWithoutExt + ".frame.txt"
                             : PathList.TsNameWithoutExt + ".part.frame.txt";
          path = Path.Combine(dir, name);
        }

        ConvertToFile.To_FrameFile(path, rawFrame, endFrame);
      }

      //TvtPlay
      if ((PathList.IsPart && 2 <= PathList.Output_Tvtp)
        || (PathList.IsLastPart && 1 <= PathList.Output_Tvtp))
      {
        string path;
        {
          string dir = (Directory.Exists(PathList.DirPath_Tvtp))
                            ? PathList.DirPath_Tvtp
                            : PathList.TsDir;
          path = Path.Combine(dir, PathList.TsNameWithoutExt + ".chapter");
        }

        ConvertToFile.To_TvtpChap(path, editFrame, endFrame);
      }

      //Ogm
      if (PathList.IsLastPart && 1 <= PathList.Output_Tvtp)
      {
        string path;
        {
          string dir = (Directory.Exists(PathList.DirPath_Misc))
                            ? PathList.DirPath_Misc
                            : PathList.TsDir;
          path = Path.Combine(dir, PathList.TsNameWithoutExt + ".ogm.chapter");
        }

        ConvertToFile.To_OgmChap(path, editFrame, endFrame);
      }
    }

  }//class
}
