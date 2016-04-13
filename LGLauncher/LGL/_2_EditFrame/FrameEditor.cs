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
   *   Edit_ConcatFrame(int[] trimFrame)
   *   {
   *     Join_Logo_Scp
   *         *.p1.jls.result.txt  -->  *.p1.frame.txt  -->  List<int>  -->  concat List<int>
   *         
   *     LogoGuillo
   *                                   *.p1.frame.txt  -->  List<int>  -->  concat List<int>
   *   }
   * 
   * 
   *   OutputChapter(List<int> rawFrame, int[] trimFrame)
   *   {
   *     concat List<int>  -->  chapter file
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
      //JLSの出力をLogoGuilloの出力と同じ形式にする。
      //  JLS Result Avs Trim   -->  frame
      if (PathList.Detector == LogoDetector.Join_Logo_Scp)
      {
        // *.p1.jls.result.avs  -->  *.p1.frame.txt
        JLS.JLS.Result_to_Frame(false);

        //合成
        //  catファイルはIsLastで使用する。
        if (PathList.IsPart)
        {
          JLS.Chapter_exe.Concat(trimFrame);
          JLS.LogoFrame.Concat(trimFrame);
        }

        //only last
        //  re-execute JLS with scpos.cat, logoframe.cat and JL_Cmd_Standard
        if (PathList.IsPart && PathList.IsLastPart)
        {
          var jl_cmd = PathList.JL_Cmd_Standard;
          var batPath = Bat_Join_Logo_Scp.Make_AtLast(jl_cmd);
          BatLuncher.Launch(batPath);
          List<int> jls_last_frame = JLS.JLS.Result_to_Frame(true);
          return jls_last_frame;
        }
      }

      //フレームテキスト合成
      //Join_Logo_Scp  &  LogoGuillo
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
        Log.WriteLine("rawFrame is null");
        return;
      }
      if (trimFrame.Count() != 2)
      {
        Log.WriteLine("trimFrame is invalid count.  Count() = " + trimFrame.Count());
        return;
      }

      int endFrame = trimFrame[1];

      //raw frame
      if (PathList.Out_RawFrame)
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
      }

      //frame
      if (PathList.Out_Frame)
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
        ConvertToFile.To_FrameFile(path, editFrame, endFrame);
      }


      //TvtPlay
      if (PathList.Out_Tvtp)
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


      //Ogm Chapter
      if (PathList.IsLastPart)
        if (PathList.Out_Tgm)
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

  }





}