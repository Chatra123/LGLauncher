using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace LGLauncher.EditFrame
{
  using OctNov.IO;

  static class Tweaker
  {
    /// <summary>
    /// 結合フレームリストを作成
    /// </summary>
    /// <remarks>
    ///  Join_Logo_Scp
    ///     生成された *.p1.jls.result.avs を *.p1.frame.txtに変換してから、読込み。
    ///  LogoGuillo
    ///     生成された *.p1.frame.txt 読込み。
    /// </remarks>
    public static List<int> ConcatFrame(int[] trimFrame)
    {
      List<int> concat = null;

      if (PathList.Detector == LogoDetector.Join_Logo_Scp)
      {
        if (PathList.PartALL == false)
        {
          //Join_Logo_Scp
          //part
          // *.jls.result.avs  -->  *.p1.frame.txt  &  List<int>
          EditFrame.JLS.Convert_JLS.ResultAvs_to_FrameFile(false);
          // フレームテキスト合成
          concat = Concat_withOldFrame(trimFrame);


          //scpos, logoframe合成
          //  作成したcatファイルはIsLastPartで使用する。
          {
            JLS.Concat_Scpos.Concat(trimFrame);
            JLS.Concat_logoframe.Concat(trimFrame);
          }

          //only last
          // re-execute JLS with scpos.cat, logoframe.cat
          if (PathList.IsLastPart)
          {
            var jl_cmdPath = PathList.JL_Cmd_Standard;
            var batPath = Bat_Join_Logo_Scp.Make_Last(jl_cmdPath);
            BatLuncher.Launch(batPath);

            // *.jls.result.avs  -->  *.last.frame.txt  &  List<int>
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
    /// 前回までのフレームリストと新たなリストをつなげる。
    /// </summary>
    private static List<int> Concat_withOldFrame(int[] trimFrame)
    {
      //avsの開始、終了フレーム番号
      //　オフセット用
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];


      //パス作成
      //logoGuilloによって作成されるファイル               *.p3.frame.txt
      string add_FramePath = PathList.WorkPath + ".frame.txt";

      //前回までの結合フレーム                             *.frame.cat.txt
      //　　 PartNo=1, PartALLでは間違えでファイルが残っていても読み込まないようにする。
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
          old_CatList = EditFrame_Convert.FrameFile_to_List(catPath);            // from  *.frame.cat.txt

          if (old_CatList == null && add_FrameList == null)
            throw new LGLException("not detect frame file");
        }

        old_CatList = old_CatList ?? new List<int>();
        add_FrameList = add_FrameList ?? new List<int>();
      }


      //
      //連結 with offset
      //　　add_FrameListがあれば連結、なければold_CatListのまま
      List<int> new_CatList = old_CatList;                                     // *.p3.frame.cat.txt
      {
        if (1 <= PathList.PartNo
          && beginFrame != int.MaxValue)
        {
          add_FrameList = add_FrameList.Select((f) => f + beginFrame).ToList();  //beginFrame分増やす
        }

        new_CatList.AddRange(add_FrameList);

        //連結部の繋ぎ目をけす。　0.5 秒以下のＣＭ除去 
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
      if (rawFrame == null || rawFrame.Count() == 0)
      {
        Log.WriteLine("rawFrame is null  or  Count() -eq 0");
        return;
      }

      if (trimFrame == null || trimFrame.Count() <= 1)
      {
        Log.WriteLine("trimFrame is null  or  Count() -le 1");
        return;
      }


      int endFrame = trimFrame[1];

      //raw frame
      if (PathList.Out_rawframe)
      {
        string frameDir = (PathList.Out_misc_toTsDir)
                           ? PathList.TsDir
                           : PathList.DirPath_misc;

        string frameName = (PathList.IsLastPart)
                          ? PathList.TsNameWithoutExt + ".rawframe.txt"
                          : PathList.TsNameWithoutExt + ".part.rawframe.txt";

        string framePath = Path.Combine(frameDir, frameName);

        EditFrame_OutChap.To_FrameFile(framePath, rawFrame, endFrame);
      }


      //短いＭａｉｎ、ＣＭをけす
      List<int> editFrame = null;
      {
        editFrame = new List<int>(rawFrame);
        editFrame = EditFrame_Convert.FlatOut_CM__(editFrame, 29.0);
        editFrame = EditFrame_Convert.FlatOut_Main(editFrame, 29.0);
      }


      //editted frame
      if (PathList.Out_frame)
      {
        string frameDir = (PathList.Out_misc_toTsDir)
                          ? PathList.TsDir
                          : PathList.DirPath_misc;

        string frameName = (PathList.IsLastPart)
                          ? PathList.TsNameWithoutExt + ".frame.txt"
                          : PathList.TsNameWithoutExt + ".part.frame.txt";

        string framePath = Path.Combine(frameDir, frameName);

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


      //NeroChap
      if (PathList.IsLastPart)
        if (PathList.Out_nero)
        {
          string chapDir = (PathList.Out_misc_toTsDir)
                            ? PathList.TsDir
                            : PathList.DirPath_misc;

          string chapPath = Path.Combine(chapDir,
                                         PathList.TsNameWithoutExt + ".nero.chapter");

          EditFrame_OutChap.To_NeroChap(chapPath, editFrame, endFrame);
        }

    }







  }
}