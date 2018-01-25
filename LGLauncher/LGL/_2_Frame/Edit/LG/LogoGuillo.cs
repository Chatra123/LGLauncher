using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;


namespace LGLauncher.Frame
{

  class LogoGuillo
  {
    /// <summary>
    /// 前回までのフレームファイルと、今回　生成したフレームをつなげる。
    /// </summary>
    public static List<int> Concat(int[] trimRange)
    {
      //logoGuilloによって作成されるファイル                    *.p3.frame.txt
      string addList_Path = PathList.WorkPath + ".frame.txt";

      //前回までのファイル                                      *.frame.cat.txt
      string catList_Path = Path.Combine(PathList.LWorkDir,
                                         PathList.TsShortName + ".frame.cat.txt");

      //読
      //　LogoGuilloが映像からロゴをみつけられない場合、*.p3.frame.txtは作成されていない。
      //　add_FrameListが見つからなくても処理は継続する。
      List<int> add_List = null, old_CatList = null;
      {
        add_List = ConvertFrame.Read_FrameFile(addList_Path);         // from  *.p3.frame.txt

        //前回までの結合フレームを取得
        if (2 <= PathList.PartNo)
        {
          old_CatList = ConvertFrame.Read_FrameFile(catList_Path);    // from  *.frame.cat.txt
          if (old_CatList == null && add_List == null)
            throw new LGLException("frame file is not found or invalid");
        }
        //ファイルが見つからないので new()
        old_CatList = old_CatList ?? new List<int>();
        add_List = add_List ?? new List<int>();
      }


      //連結 with offset
      List<int> new_CatList;
      {
        new_CatList = new List<int>(old_CatList);
        if (trimRange != null)
        {
          int beginFrame = trimRange[0];
          add_List = add_List.Select((f) => f + beginFrame).ToList();
        }
        new_CatList.AddRange(add_List);
        //連結部の繋ぎ目をけす
        new_CatList = ConvertFrame.FlatOut_CM__(new_CatList, 0.5);
      }


      //List<string>  <--  List<int>
      var new_CatText = new_CatList.Select(f => f.ToString()).ToList();
      //書
      //次回の参照用に上書き                    *.frame.cat.txt
      File.WriteAllLines(catList_Path, new_CatText, TextEnc.Shift_JIS);
      //catPath_partはDetect_PartNo_fromFileName()で使用されるので必ず作成すること。
      //                                        *.p3.frame.cat.txt
      string catPath_part = PathList.WorkPath + ".frame.cat.txt";
      File.WriteAllLines(catPath_part, new_CatText, TextEnc.Shift_JIS);

      return new_CatList;
    }

  }

}