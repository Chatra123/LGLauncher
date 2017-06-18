using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;


namespace LGLauncher.Frame
{
  using OctNov.IO;

  class LogoGuillo
  {
    /// <summary>
    /// 前回のフレームファイルと、今回　生成したリストをつなげる。
    /// </summary>
    public static List<int> Concat(int[] trimFrame)
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
        add_FrameList = ConvertFrame.Read_FrameFile(add_FramePath);    // from  *.p3.frame.txt

        //前回までの結合フレームを取得
        if (2 <= PathList.PartNo)
        {
          old_CatList = ConvertFrame.Read_FrameFile(catPath);          // from  *.frame.cat.txt
          if (old_CatList == null && add_FrameList == null)
            throw new LGLException("not found frame file  or  is invalid");
        }
        //ファイルが見つからないと new()
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
      //次回の参照用                            *.frame.cat.txt
      File.WriteAllLines(catPath, new_CatText, TextEnc.Shift_JIS);

      //catPath_partはDetect_PartNo()で使用されるので必ず作成すること。
      //                                        *.p3.frame.cat.txt
      string catPath_part = PathList.WorkPath + ".frame.cat.txt";
      File.WriteAllLines(catPath_part, new_CatText, TextEnc.Shift_JIS);

      return new_CatList;
    }



  }

}