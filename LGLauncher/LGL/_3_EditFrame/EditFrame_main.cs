using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace LGLauncher
{
  static class EditFrame_main
  {
    /// <summary>
    /// 前回までのフレームリストと新たなリストをつなげる。
    /// </summary>
    /// <returns></returns>
    public static void Concat(int[] trimFrame)
    {
      //パス作成
      //logoGuilloにより作成されるFrame          *.p3.frame.txt
      string addFramePath = PathList.WorkPath + ".frame.txt";

      //前回のcatframe                           *.p2.catframe.txt
      string oldCatPath = "";

      //初回 or 全体処理
      if (PathList.No == 1 || PathList.No == -1)
        oldCatPath = "";                                                       //前回のcatframeはない
      else if (2 <= PathList.No)
      {
        //PartNoより前も検索する。
        //　何らかのエラーで catframe.txtを作成されていなかったとしても、
        //　後続のLGLauncherの処理を止めないため。
        for (int i = PathList.No - 1; 0 < i; i--)
        {
          oldCatPath = Path.Combine(PathList.LWorkDir,
                                       PathList.TsShortName + ".p" + i + ".catframe.txt");
          if (File.Exists(oldCatPath)) break;                                  //found
        }
      }


      //新しく作るcatframe                       *.p3.catframe.txt
      string newCatPath = PathList.WorkPath + ".catframe.txt";


      //ファイル名からフレーム数取得
      //  avsの開始、終了フレーム番号
      //  デバッグしやすいので０でなくint.MaxValueを初期値にする。
      int beginFrame = int.MaxValue, endFrame = int.MaxValue;
      if (1 <= PathList.No)
      {
        if (trimFrame != null)
        {
          beginFrame = trimFrame[0];
          endFrame = trimFrame[1];
        }
        else
        {
          //映像が短くd2vが作成されていないとフレーム数取得はできない
          Log.WriteLine("trimFrame == null,  not detect begin frame");
        }
      }


      //
      //フレーム読込み
      //　addFrameListがなくても処理は継続する。
      //　LogoGuilloが映像からロゴをみつけられない場合、*.p6.frame.txtは作成されていない
      List<int> oldCatList = GetFrameList(oldCatPath);                         // *.p2.catframe.txt

      List<int> addFrameList = GetFrameList(addFramePath);                     // *.p3.frame.txt

      if (oldCatList == null && addFrameList == null)
        throw new LGLException("not detect frame file");

      oldCatList = oldCatList ?? new List<int>();


      //
      //連結 with offset
      //　　addFrameListがあれば連結、なければnewCatListのまま
      List<int> newCatList = oldCatList;                                        //*.p3.catframe.txt

      if (addFrameList != null)
      {
        if (1 <= PathList.No && beginFrame != int.MaxValue)
          addFrameList = addFrameList.Select((f) => f + beginFrame).ToList();  //beginFrame分増やす

        newCatList.AddRange(addFrameList);
      }


      //フレームリストの繋ぎ目をけす。　0.5 秒以下のＣＭ除去 
      newCatList = EditFrame_sub.FlatOut_CM__(newCatList, 0.5);

      if (newCatList == null)
        throw new LGLException("frame edit error1");


      //frame書込み
      var newCatText = newCatList.Select(f => f.ToString()).ToList();          //List<int>  -->  List<string>に変換

      File.WriteAllLines(newCatPath, newCatText, TextEnc.Shift_JIS);


      //フレームを外部フォルダに出力
      OutFrameFile(newCatList, endFrame);


      //
      //TvtPlay用に編集
      //　短いＭａｉｎ、ＣＭをけす
      var editList = newCatList;
      editList = EditFrame_sub.FlatOut_CM__(editList, 29.0);
      editList = EditFrame_sub.FlatOut_Main(editList, 29.0);

      if (editList == null)
        throw new LGLException("frame edit error2");


      //終端がＣＭの途中か？
      //　　終端がＣＭの途中　→　スキップ用にendFrame追加
      //　　終端が本編の途中　→　最後のフレーム削除
      if (1 <= PathList.No && 0 < editList.Count)
      {
        int main_endframeNo = editList[editList.Count - 1];
        bool end_is_CM = (main_endframeNo != endFrame);                        //(mainの終端！＝avsの終端)

        if (end_is_CM)
          editList.Add(endFrame);
        else
          editList.RemoveAt(editList.Count - 1);
      }


      //TvtPlay用chapter出力
      {
        string chapText = EditFrame_sub.ConvertToTvtPlayChap(editList);

        string chapPath = Path.Combine(PathList.ChapDir,
                                       PathList.TsNameWithoutExt + ".chapter");
        try
        {
          if (Directory.Exists(PathList.ChapDir))
            File.WriteAllText(chapPath, chapText, TextEnc.UTF8_bom);
        }
        catch
        {
          Log.WriteLine("write error on ChapDir");
          Log.WriteLine("  PathList.ChapDir = " + PathList.ChapDir);
        }
      }

    }

    /// <summary>
    /// フレームファイルを取得する。
    /// </summary>
    /// <param name="framePath">取得するフレームファイルパス</param>
    /// <returns>
    /// 取得成功　→　リストをList<int>で返す。
    /// 　　失敗　→　null
    /// </returns>
    private static List<int> GetFrameList(string framePath)
    {
      //convert List<string>  to  List<int>
      var ConvertToIntList = new Func<List<string>, List<int>>(
        (stringList) =>
        {
          var intList = new List<int>();
          int result;

          foreach (var str in stringList)
          {
            string line = str.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;                     //空白ならスキップ
            if (int.TryParse(line, out result) == false) return null;          //変換失敗
            intList.Add(result);
          }

          return intList;
        });

      //読込み
      if (File.Exists(framePath) == false) return null;    //ファイルチェック
      var frameText = FileR.ReadAllLines(framePath);       //List<string>でファイル取得
      if (frameText == null) return null;

      //List<int>に変換
      var frameList = ConvertToIntList(frameText);

      //エラーチェック
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;           //奇数ならエラー
      return frameList;
    }


    /// <summary>
    /// フレームを外部フォルダに出力
    /// </summary>
    private static void OutFrameFile(List<int> frameList, int endFrame)
    {
      var frameText = new StringBuilder();

      {
        //付加情報
        //　終端がＣＭの途中か？
        var end_is_CM = "0";
        if (0 < frameList.Count)
        {
          int main_endframeNo = frameList[frameList.Count - 1];
          end_is_CM = (main_endframeNo != endFrame) ? "1" : "0";             //(mainの終端！＝avsの終端)
        }
        var is_last_file = PathList.Mode_IsLast ? "1" : "0";

        frameText.AppendLine("// end_frame_no=" + endFrame);
        frameText.AppendLine("//    end_is_cm=" + end_is_CM);
        frameText.AppendLine("// is_last_file=" + is_last_file);

        foreach (var f in frameList)
          frameText.AppendLine(f.ToString());
      }

      string frameExt = (PathList.Mode_IsLast) ? ".frame.txt" : ".partframe.txt";
      string framePath = Path.Combine(PathList.FrameDir, PathList.TsName + frameExt);

      try
      {
        if (Directory.Exists(PathList.FrameDir))
          File.WriteAllText(framePath, frameText.ToString(), TextEnc.Shift_JIS);
      }
      catch
      {
        Log.WriteLine("write error on FrameDir");
        Log.WriteLine("  PathList.FrameDir = " + PathList.FrameDir);
      }
    }


  }//class
}