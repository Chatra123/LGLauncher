using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace LGLauncher.EditFrame
{
  using OctNov.IO;


  /*
   * ConcatFrame(int[] trimFrame)
   * 
   *     Join_Logo_Scp
   *         *.p1.jls.result.txt  -->  *.p1.frame.txt  -->  List<int>  -->  concat frame file
   *         
   *     LogoGuillo
   *                                   *.p1.frame.txt  -->  List<int>  -->  concat frame file
   * 
   * 
   *  OutChap(List<int> rawFrame, int[] trimFrame)
   * 
   *     List<int>  -->  chapter file
   * 
   */


  static class FrameEditor
  {
    /// <summary>
    /// 結合フレームリストを作成
    /// </summary>
    public static List<int> ConcatFrame(int[] trimFrame)
    {
      List<int> concat = null;

      if (PathList.Detector == LogoDetector.Join_Logo_Scp)
      {
        if (PathList.IsAll == false)
        {
          //Join_Logo_Scp
          //part
          // *.jls.result.avs  -->  *.p1.frame.txt
          EditFrame.JLS.Convert_JLS.ResultAvs_to_FrameFile(false);

          // *.frame.cat.txt合成
          concat = Concat_withOldFrame(trimFrame);

          //cat合成
          //  catファイルはIsLastPartで使用する。
          {
            JLS.Concat_Scpos.Concat(trimFrame);
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
        else
        {
          //Join_Logo_Scp
          //all
          // *.jls.result.avs  -->  *.all.frame.txt
          concat = EditFrame.JLS.Convert_JLS.ResultAvs_to_FrameFile(false);
        }
      }
      else
      {
        //LogoGuillo
        //part & all
        //フレームテキスト合成
        concat = Concat_withOldFrame(trimFrame);
      }

      return concat;
    }



    /// <summary>
    /// 前回までのフレームリストと、今回　生成したリストをつなげる。
    /// </summary>
    private static List<int> Concat_withOldFrame(int[] trimFrame)
    {

      //パス作成
      //logoGuilloによって作成されるファイル               *.p3.frame.txt
      string add_FramePath = PathList.WorkPath + ".frame.txt";

      //前回までの結合フレーム                             *.frame.cat.txt
      //　　 PartNo=1, PartALLではエラーなどでファイルが残っていても読み込まないようにする。
      string catPath = Path.Combine(PathList.LWorkDir,
                                    PathList.TsShortName + ".frame.cat.txt");

      //
      //フレーム読込み
      //　LogoGuilloが映像からロゴをみつけられない場合、*.p3.frame.txtは作成されていない
      //　add_FrameListが見つからなくても処理は継続する。
      List<int> add_FrameList = null, old_CatList = null;
      {
        add_FrameList = EditFrame_Convert.FrameFile_to_List(add_FramePath);    // from  *.p3.frame.txt
        if (2 <= PathList.PartNo)
        {
          old_CatList = EditFrame_Convert.FrameFile_to_List(catPath);          // from  *.frame.cat.txt

          if (old_CatList == null && add_FrameList == null)
            throw new LGLException("not detect frame file");
        }
        old_CatList = old_CatList ?? new List<int>();
        add_FrameList = add_FrameList ?? new List<int>();
      }


      //
      //連結 with offset
      List<int> new_CatList = old_CatList;                                     // *.p3.frame.cat.txt
      {
        if (PathList.IsPart && trimFrame != null)
        {
          int beginFrame = trimFrame[0];
          add_FrameList = add_FrameList.Select((f) => f + beginFrame).ToList();
        }
        new_CatList.AddRange(add_FrameList);
        //連結部の繋ぎ目をけす。
        new_CatList = EditFrame_Convert.FlatOut_CM__(new_CatList, 0.5);
      }


      //List<string>  <--  List<int>
      var new_CatText = new_CatList.Select(f => f.ToString()).ToList();

      //書込み
      {
        //次回の参照用
        File.WriteAllLines(catPath, new_CatText, TextEnc.Shift_JIS);
        //デバッグ記録用
        string catPath_debug = PathList.WorkPath + ".frame.cat.txt";
        File.WriteAllLines(catPath_debug, new_CatText, TextEnc.Shift_JIS);
      }
      return new_CatList;
    }


    /// <summary>
    /// チャプター出力
    /// </summary>
    public static void OutChap(List<int> rawFrame, int[] trimFrame)
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
      if (PathList.Out_rawframe)
      {
        string frameDir = (PathList.Out_misc_toTsDir)
                           ? PathList.TsDir
                           : PathList.DirPath_misc;
        string framePath = Path.Combine(frameDir,
                                        PathList.TsNameWithoutExt + ".rawframe.txt");
        EditFrame_OutChap.To_FrameFile(framePath, rawFrame, endFrame);
      }

      //短いＭａｉｎ、ＣＭを潰す
      List<int> editFrame = null;
      {
        editFrame = new List<int>(rawFrame);
        editFrame = EditFrame_Convert.FlatOut_CM__(editFrame, PathList.Regard_NsecCM_AsMain);
        editFrame = EditFrame_Convert.FlatOut_Main(editFrame, PathList.Regard_NsecMain_AsCM);
      }

      //edited frame
      if (PathList.Out_frame)
      {
        string frameDir = (PathList.Out_misc_toTsDir)
                          ? PathList.TsDir
                          : PathList.DirPath_misc;
        string framePath = Path.Combine(frameDir,
                                        PathList.TsNameWithoutExt + ".frame.txt");
        EditFrame_OutChap.To_FrameFile(framePath, editFrame, endFrame);
      }


      //TvtPlay
      if (PathList.Out_tvtp)
      {
        string chapDir = (PathList.Out_tvtp_toTsDir)
                          ? PathList.TsDir
                          : PathList.DirPath_tvtp;
        string chapPath = Path.Combine(chapDir,
                                       PathList.TsNameWithoutExt + ".chapter");
        EditFrame_OutChap.To_TvtPlayChap(chapPath, editFrame, endFrame);
      }


      //OgmChap
      if (PathList.IsLastPart)
        if (PathList.Out_ogm)
        {
          string chapDir = (PathList.Out_misc_toTsDir)
                            ? PathList.TsDir
                            : PathList.DirPath_misc;
          string chapPath = Path.Combine(chapDir,
                                         PathList.TsNameWithoutExt + ".ogm.chapter");
          EditFrame_OutChap.To_OgmChap(chapPath, editFrame, endFrame);
        }

    }







  }
}