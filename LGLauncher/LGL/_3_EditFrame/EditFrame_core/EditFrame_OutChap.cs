﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace LGLauncher.EditFrame
{
  using OctNov.IO;

  static class EditFrame_OutChap
  {

    /// <summary>
    /// フレームを外部フォルダに出力
    /// </summary>
    public static void To_FrameFile(string outPath, List<int> framelist, int endFrame)
    {
      if (framelist == null || framelist.Count == 0) return;

      var frameText = new StringBuilder();
      {
        //付加情報
        //　終端はＣＭ？
        var end_is_CM = "";
        int main_endframeNo = framelist[framelist.Count - 1];
        end_is_CM = (main_endframeNo != endFrame) ? "1" : "0";     //(mainの終端！＝avsの終端)
        var is_last_file = PathList.IsLastPart ? "1" : "0";

        frameText.AppendLine("//    end_frame=" + endFrame);
        frameText.AppendLine("//    end_is_cm=" + end_is_CM);
        frameText.AppendLine("//      part_no=" + PathList.PartNo);
        frameText.AppendLine("// is_last_file=" + is_last_file);

        foreach (var f in framelist)
          frameText.AppendLine(f.ToString());
      }

      try
      {
        string dirPath = Path.GetDirectoryName(outPath);

        if (Directory.Exists(dirPath))
          File.WriteAllText(outPath, frameText.ToString(), TextEnc.Shift_JIS);
      }
      catch
      {
        Log.WriteLine("write error");
        Log.WriteLine("      " + outPath);
      }
    }



    /// <summary>
    /// TvtPlay用chapter出力
    /// </summary>
    public static void To_TvtPlayChap(string outPath, List<int> framelist, int endFrame)
    {
      if (framelist == null || framelist.Count == 0) return;

      var chapList = new List<int>(framelist);  //ディープコピー

      //TvtPlay用に少し加工
      //終端はＣＭ？
      //　　終端がＣＭの途中　→　スキップ用にendFrame追加
      //　　終端が本編の途中　→　最後のフレーム削除
      if (PathList.IsLastPart == false)
      {
        int main_endframeNo = chapList[chapList.Count - 1];
        bool end_is_CM = (main_endframeNo != endFrame);              //(mainの終端！＝avsの終端)

        if (end_is_CM)
          chapList.Add(endFrame);
        else
          chapList.RemoveAt(chapList.Count - 1);
      }


      string chapText = EditFrame_Convert.To_TvtPlayChap(chapList);

      try
      {
        string dirPath = Path.GetDirectoryName(outPath);

        if (Directory.Exists(dirPath))
          File.WriteAllText(outPath, chapText, TextEnc.UTF8_bom);
      }
      catch
      {
        Log.WriteLine("write error");
        Log.WriteLine("      " + outPath);
      }
    }



    /// <summary>
    /// OgmChapを出力
    /// </summary>
    public static void To_OgmChap(string outPath, List<int> framelist, int endFrame)
    {
      if (framelist == null || framelist.Count == 0) return;

      //OgmChap用に少し加工

      //本編の開始のみを抽出、ＣＭの開始を除去   　Linqでディープコピー
      var chaplist = framelist.Where((frameNo, index) => index % 2 == 0).ToList();

      //00:00:00にChap追加
      if (30 * 3 <= framelist[0])
        chaplist.Insert(0, 0);

      //最後から３秒前にChap追加
      //　５分以上、IsLastPartのみ
      if (PathList.IsLastPart)
      {
        if (30 * 60 * 5 <= endFrame)
          chaplist.Add(endFrame - 30 * 3);
      }


      string chapText = EditFrame_Convert.To_OgmChap_type2(chaplist);

      try
      {
        string dirPath = Path.GetDirectoryName(outPath);

        if (Directory.Exists(dirPath))
          File.WriteAllText(outPath, chapText.ToString(), TextEnc.Shift_JIS);
      }
      catch
      {
        Log.WriteLine("write error");
        Log.WriteLine("      " + outPath);
      }
    }



  }
}