using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;


namespace LGLauncher.Frame
{
  using OctNov.IO;

  /// <summary>
  /// frameList --> 出力用テキストを作成
  /// </summary>
  static class MakeChapText
  {
    /// <summary>
    /// frameList --> Output frame text
    /// </summary>
    public static string Make_Frame(List<int> frameList, int endFrame)
    {
      if (frameList == null || frameList.Count == 0) return null;

      var text = new StringBuilder();
      {
        //録画中
        if (PathList.IsLastPart == false)
        {
          //付加情報
          int main_endframeNo = frameList[frameList.Count - 1];
          string end_is_CM = main_endframeNo < endFrame ? "1" : "0";   //本編部の終端＜avsの終端
          string is_last_file = PathList.IsLastPart ? "1" : "0";
          text.AppendLine("//    end_frame=" + endFrame);
          text.AppendLine("//    end_is_cm=" + end_is_CM);
          text.AppendLine("//      part_no=" + PathList.PartNo);
          text.AppendLine("// is_last_file=" + is_last_file);
        }

        foreach (int f in frameList)
          text.AppendLine(f.ToString());
      }

      return text.ToString();
    }


    /// <summary>
    /// frameList --> Tvtp Chapter text
    /// </summary>
    public static string Make_Tvtp(List<int> frameList, int endFrame)
    {
      if (frameList == null || frameList.Count == 0) return null;

      string text = "";
      {
        List<int> chapList = new List<int>(frameList);       //コピー

        if (PathList.IsLastPart)
        {
          //録画後
          text = ConvertFrame.To_TvtpChap(chapList);
        }
        else//録画中
        {
          //Avs終端はＣＭ？
          //　終端がＣＭの途中  -->  スキップ用にendFrame追加、その後は本編と仮定
          //　終端が本編の途中  -->  最後のフレーム削除、本編はまだ続くと仮定
          int main_endframeNo = chapList[chapList.Count - 1];
          bool end_is_CM = main_endframeNo < endFrame;       //本編部の終端＜avsの終端
          if (end_is_CM)
            chapList.Add(endFrame);
          else
            chapList.RemoveAt(chapList.Count - 1);

          text = ConvertFrame.To_TvtpChap(chapList);
          text = text.Substring(0, text.Length - "0eox-c".Length);
          text += "c";    //録画中なら末尾の 0eox はつけない
        }
      }

      return text;
    }



    /// <summary>
    /// frameList --> Ogm Chapter text
    /// </summary>
    public static string Make_Ogm(List<int> frameList, int endFrame)
    {
      if (frameList == null || frameList.Count == 0) return null;

      string text = "";
      {
        List<int> chapList = new List<int>(frameList);       //コピー
        text = ConvertFrame.To_OgmChap_type1(chapList);
      }

      return text;
    }



  }
}