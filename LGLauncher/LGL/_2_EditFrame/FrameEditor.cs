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
    public static List<int> Edit_ConcatFrame(int[] trimFrame)
    {
      List<int> concat = null;

      //Join_Logo_Scp
      if (PathList.Detector == LogoDetector.Join_Logo_Scp)
      {
        if (PathList.IsPart)
        {
          //IsPart
          // *.jls.result.avs  -->  *.p1.frame.txt
          EditFrame.JLS.Convert_JLS.ResultAvs_to_FrameFile(false);

          // *.frame.cat.txt合成
          concat = Concat_withPreviousFrame(trimFrame);

          //cat合成
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
        else
        {
          //IsAll
          // *.jls.result.avs  -->  *.all.frame.txt
          concat = EditFrame.JLS.Convert_JLS.ResultAvs_to_FrameFile(false);
        }
      }

      //LogoGuillo
      if (PathList.Detector == LogoDetector.LogoGuillo)
      {
        //part & all
        //フレームテキスト合成
        concat = Concat_withPreviousFrame(trimFrame);
      }

      return concat;
    }



    /// <summary>
    /// 前回までのフレームリストと、今回　生成したリストをつなげる。
    /// </summary>
    private static List<int> Concat_withPreviousFrame(int[] trimFrame)
    {
      //logoGuilloによって作成されるファイル               *.p3.frame.txt
      string add_FramePath = PathList.WorkPath + ".frame.txt";

      //結合フレーム                                       *.frame.cat.txt
      string catPath = Path.Combine(PathList.LWorkDir,
                                    PathList.TsShortName + ".frame.cat.txt");

      //読
      //　LogoGuilloが映像からロゴをみつけられない場合、*.p3.frame.txtは作成されていない。
      //　add_FrameListが見つからなくても処理は継続する。
      List<int> add_FrameList = null, old_CatList = null;
      {
        add_FrameList = ConvertFrame.FrameFile_to_List(add_FramePath);    // from  *.p3.frame.txt

        //前回までの結合フレームを取得
        if (2 <= PathList.PartNo)
        {
          old_CatList = ConvertFrame.FrameFile_to_List(catPath);          // from  *.frame.cat.txt
          if (old_CatList == null && add_FrameList == null)
            throw new LGLException("not detect frame file  or  is invalid file");
        }
        //ファイルがなければnullが返されているのでnew()。
        old_CatList = old_CatList ?? new List<int>();
        add_FrameList = add_FrameList ?? new List<int>();
      }


      //連結 with offset
      List<int> new_CatList;
      {
        new_CatList = new List<int>(old_CatList);

        if (PathList.IsPart && trimFrame != null)
        {
          int beginFrame = trimFrame[0];
          add_FrameList = add_FrameList.Select((f) => f + beginFrame).ToList();
        }

        new_CatList.AddRange(add_FrameList);
        //連結部の繋ぎ目をけす。
        new_CatList = ConvertFrame.FlatOut_CM__(new_CatList, 0.5);
      }


      //List<string>  <--  List<int>
      var new_CatText = new_CatList.Select(f => f.ToString()).ToList();

      //書
      {
        //次回の参照用                          *.frame.cat.txt
        File.WriteAllLines(catPath, new_CatText, TextEnc.Shift_JIS);
        //デバッグ用記録                        *.p3.frame.cat.txt
        string catPath_debug = PathList.WorkPath + ".frame.cat.txt";
        File.WriteAllLines(catPath_debug, new_CatText, TextEnc.Shift_JIS);
      }
      return new_CatList;
    }


    /// <summary>
    /// チャプター出力
    /// </summary>
    public static void Edit_Chapter(List<int> rawFrame, int[] trimFrame)
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
        string path;
        {
          string dir = (PathList.Out_misc_toTsDir)
                             ? PathList.TsDir
                             : PathList.DirPath_misc;
          string name = (PathList.IsLastPart)
                             ? PathList.TsNameWithoutExt + ".rawframe.txt"
                             : PathList.TsNameWithoutExt + ".part.rawframe.txt";
          path = Path.Combine(dir, name);
        }
        OutputChapter.To_FrameFile(path, rawFrame, endFrame);
      }


      //短いＭａｉｎ、ＣＭを潰す
      List<int> editFrame = null;
      {
        editFrame = new List<int>(rawFrame);
        editFrame = ConvertFrame.FlatOut_CM__(editFrame, PathList.Regard_NsecCM_AsMain);
        editFrame = ConvertFrame.FlatOut_Main(editFrame, PathList.Regard_NsecMain_AsCM);
      }

      //frame
      if (PathList.Out_frame)
      {
        string path;
        {
          string dir = (PathList.Out_misc_toTsDir)
                             ? PathList.TsDir
                             : PathList.DirPath_misc;
          string name = (PathList.IsLastPart)
                             ? PathList.TsNameWithoutExt + ".frame.txt"
                             : PathList.TsNameWithoutExt + ".part.frame.txt";
          path = Path.Combine(dir, name);
        }
        OutputChapter.To_FrameFile(path, editFrame, endFrame);
      }


      //TvtPlay
      if (PathList.Out_tvtp)
      {
        string path;
        {
          string dir = (PathList.Out_tvtp_toTsDir)
                            ? PathList.TsDir
                            : PathList.DirPath_tvtp;
          path = Path.Combine(dir, PathList.TsNameWithoutExt + ".chapter");
        }
        OutputChapter.To_TvtPlayChap(path, editFrame, endFrame);
      }


      //OgmChap
      if (PathList.IsLastPart)
        if (PathList.Out_ogm)
        {
          string path;
          {
            string dir = (PathList.Out_misc_toTsDir)
                              ? PathList.TsDir
                              : PathList.DirPath_misc;
            path = Path.Combine(dir, PathList.TsNameWithoutExt + ".ogm.chapter");
          }
          OutputChapter.To_OgmChap(path, editFrame, endFrame);
        }
    }







  }
}