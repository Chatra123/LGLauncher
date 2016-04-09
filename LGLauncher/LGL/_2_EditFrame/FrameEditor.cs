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
   * Edit_ConcatFrame(int[] trimFrame)
   * 
   *     Join_Logo_Scp
   *         *.p1.jls.result.txt  -->  *.p1.frame.txt  -->  List<int>  -->  concat frame file
   *         
   *     LogoGuillo
   *                                   *.p1.frame.txt  -->  List<int>  -->  concat frame file
   * 
   * 
   *  Edit_Chapter(List<int> rawFrame, int[] trimFrame)
   * 
   *     List<int>  -->  chapter file
   * 
   */


  static class FrameEditor
  {
    /// <summary>
    /// 結合フレームリストを作成
    /// </summary>
    public static List<int> Edit_Concat(int[] trimFrame)
    {
      List<int> concat = null;

      //Join_Logo_Scp
      if (PathList.Detector == LogoDetector.Join_Logo_Scp)
      {
        if (PathList.IsPart)
        {
          //IsPart
          // *.jls.result.avs  -->  *.p1.frame.txt
          JLS.Convert_JLS.ResultAvs_to_FrameFile(false);

          // *.frame.cat.txt合成
          concat = Concat_Frame.Concat(trimFrame);

          //合成
          //  catファイルはIsLastで使用する。
          {
            JLS.Concat_scpos.Concat(trimFrame);
            JLS.Concat_logoframe.Concat(trimFrame);
          }

          //only last
          // re-execute JLS with scpos.cat, logoframe.cat
          if (PathList.IsLastPart)
          {
            var jl_cmdPath = PathList.JL_Cmd_Standard;
            var batPath = Bat_Join_Logo_Scp.Make_AtLast(jl_cmdPath);
            BatLuncher.Launch(batPath);

            // *.jls.result.txt  -->  *.last.frame.txt  &  List<int>
            concat = JLS.Convert_JLS.ResultAvs_to_FrameFile(true);
          }
        }
        else //IsAll
        {
          // *.jls.result.avs  -->  *.all.frame.txt
          concat = JLS.Convert_JLS.ResultAvs_to_FrameFile(false);
        }
      }

      //LogoGuillo
      if (PathList.Detector == LogoDetector.LogoGuillo)
      {
        //IsPart & IsAll
        //フレームテキスト合成
        concat = Concat_Frame.Concat(trimFrame);
      }

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
      else if (trimFrame.Count() != 2)
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
        editFrame = EditFrameList.FlatOut_CM__(editFrame, PathList.Regard_NsecCM_AsMain);
        editFrame = EditFrameList.FlatOut_Main(editFrame, PathList.Regard_NsecMain_AsCM);
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
        ConvertToFile.To_TvtPlayChap(path, editFrame, endFrame);
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